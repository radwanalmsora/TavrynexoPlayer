using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shell;
using System.Windows.Threading;
using LibVLCSharp.Shared;
using LibVLCSharp.Shared.Structures;
using Microsoft.Win32;
using TavrynexoPlayer.Models;
using TavrynexoPlayer.Services;
using VlcPlayer = LibVLCSharp.Shared.MediaPlayer;

namespace TavrynexoPlayer;

public partial class MainWindow : Window
{
    // ------------------------------------------------------------------ state
    private static readonly HashSet<string> MediaExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mp4", ".mkv", ".avi", ".mov", ".wmv", ".flv", ".webm", ".m4v", ".mpg", ".mpeg", ".ts", ".m2ts",
        ".3gp", ".ogv", ".vob", ".mts", ".mp3", ".flac", ".wav", ".aac", ".m4a", ".ogg", ".opus", ".wma",
        ".ape", ".aiff", ".mka", ".m3u8"
    };

    private static readonly float[] SpeedSteps = { 0.25f, 0.5f, 0.75f, 1f, 1.25f, 1.5f, 2f, 3f, 4f };

    private static readonly Brush ClickCatcher = MakeFrozen(Color.FromArgb(1, 0, 0, 0));

    private readonly AppSettings _settings = SettingsService.Load();
    private readonly ObservableCollection<PlaylistItem> _playlist = new();
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMilliseconds(250) };
    private readonly DispatcherTimer _idleTimer = new() { Interval = TimeSpan.FromMilliseconds(2500) };
    private readonly DispatcherTimer _clickTimer = new() { Interval = TimeSpan.FromMilliseconds(230) };
    private readonly DispatcherTimer _osdTimer = new() { Interval = TimeSpan.FromMilliseconds(1400) };
    private readonly Random _rng = new();

    private LibVLC? _libVLC;
    private VlcPlayer? _player;
    private int _index = -1;
    private string? _currentKey;
    private long _pendingSeekMs = -1;
    private float _rate = 1f;
    private int _mode = 1; // 0 = video, 1 = idle splash, 2 = audio-only
    private bool _seekDragging;
    private bool _isFullscreen;
    private bool _wasMaximized;
    private Rect _restoreBounds;
    private Point _lastMouse;

    private static Brush MakeFrozen(Color c)
    {
        var b = new SolidColorBrush(c);
        b.Freeze();
        return b;
    }

    // ------------------------------------------------------------ construction
    public MainWindow()
    {
        InitializeComponent();

        PlaylistList.ItemsSource = _playlist;
        ShuffleButton.IsChecked = _settings.Shuffle;
        ApplyRepeatIcon();

        SeekSlider.AddHandler(PreviewMouseLeftButtonDownEvent, new MouseButtonEventHandler((_, _) => _seekDragging = true), true);
        SeekSlider.AddHandler(PreviewMouseLeftButtonUpEvent, new MouseButtonEventHandler((_, _) => _seekDragging = false), true);
        SeekSlider.LostMouseCapture += (_, _) => _seekDragging = false;

        _timer.Tick += (_, _) => OnTick();
        _idleTimer.Tick += (_, _) => OnIdle();
        _osdTimer.Tick += (_, _) => { _osdTimer.Stop(); OsdBorder.Visibility = Visibility.Collapsed; };
        _clickTimer.Tick += (_, _) => { _clickTimer.Stop(); if (_mode != 1) TogglePause(); };

        StateChanged += (_, _) => UpdateWindowState();
        Loaded += OnLoaded;
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        SetRoundedCorners(true);
        try
        {
            int dark = 1;
            Native.DwmSetWindowAttribute(new WindowInteropHelper(this).Handle, 20, ref dark, sizeof(int));
        }
        catch { /* older builds */ }
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _libVLC = new LibVLC("--no-video-title-show");
        _player = new VlcPlayer(_libVLC)
        {
            EnableMouseInput = false,
            EnableKeyInput = false
        };
        VideoView.MediaPlayer = _player;

        _player.EndReached += (_, _) => Dispatcher.BeginInvoke(new Action(OnEnded));
        _player.EncounteredError += (_, _) => Dispatcher.BeginInvoke(new Action(() => ShowOsd("Cannot play this media")));

        VolumeSlider.Value = _settings.Volume;
        _player.Volume = _settings.Volume;
        _player.Mute = _settings.Muted;
        VolumeText.Text = $"{_settings.Volume}%";
        UpdateMuteIcon();

        if (_settings.ShowPlaylist) SetPlaylistVisible(true);
        _timer.Start();

        if (!string.IsNullOrWhiteSpace(App.StartupFile)) OpenPaths(new[] { App.StartupFile! }, append: false);
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        SavePosition();
        _settings.ShowPlaylist = PlaylistPanel.Visibility == Visibility.Visible;
        if (_player != null) { _settings.Volume = _player.Volume; _settings.Muted = _player.Mute; }
        SettingsService.Save(_settings);
        base.OnClosing(e);
    }

    protected override void OnClosed(EventArgs e)
    {
        _timer.Stop();
        var p = _player; var l = _libVLC;
        _player = null; _libVLC = null;
        VideoView.MediaPlayer = null;
        Task.Run(() => { try { p?.Stop(); p?.Dispose(); l?.Dispose(); } catch { } });
        base.OnClosed(e);
    }

    // ----------------------------------------------------------------- helpers
    private static string Fmt(long ms)
    {
        if (ms < 0) ms = 0;
        var t = TimeSpan.FromMilliseconds(ms);
        return t.TotalHours >= 1 ? $"{(int)t.TotalHours}:{t.Minutes:00}:{t.Seconds:00}" : $"{t.Minutes}:{t.Seconds:00}";
    }

    private static bool IsMedia(string path) => MediaExtensions.Contains(Path.GetExtension(path));

    private void ShowOsd(string text)
    {
        OsdText.Text = text;
        OsdBorder.Visibility = Visibility.Visible;
        _osdTimer.Stop();
        _osdTimer.Start();
    }

    private void SetMode(int mode)
    {
        if (_mode == mode) return;
        _mode = mode;
        IdlePanel.Visibility = mode == 1 ? Visibility.Visible : Visibility.Collapsed;
        AudioPanel.Visibility = mode == 2 ? Visibility.Visible : Visibility.Collapsed;
        Overlay.Background = mode == 0 ? ClickCatcher : (Brush)FindResource("BgBrush");
    }

    private void UpdatePlayState()
    {
        PlayButton.Content = _player?.IsPlaying == true ? "\uE769" : "\uE768";
    }

    private void UpdateMuteIcon()
    {
        MuteButton.Content = _player?.Mute == true || VolumeSlider.Value <= 0 ? "\uE74F" : "\uE767";
    }

    private void ApplyRepeatIcon()
    {
        RepeatButton.Content = _settings.RepeatMode == 2 ? "\uE8ED" : "\uE8EE";
        RepeatButton.IsChecked = _settings.RepeatMode != 0;
    }

    private void UpdateWindowState()
    {
        MaxButton.Content = WindowState == WindowState.Maximized ? "\uE923" : "\uE922";
        Root.Margin = WindowState == WindowState.Maximized && !_isFullscreen
            ? SystemParameters.WindowResizeBorderThickness
            : new Thickness(0);
    }

    // -------------------------------------------------------------- tick / UI
    private void OnTick()
    {
        var p = _player;
        if (p == null) return;

        UpdatePlayState();
        var state = p.State;
        if (state is VLCState.Stopped or VLCState.NothingSpecial or VLCState.Ended or VLCState.Error) return;

        long len = p.Length;
        long t = p.Time;

        if (_pendingSeekMs > 0 && len > 0 && p.IsSeekable && state == VLCState.Playing)
        {
            p.Time = _pendingSeekMs;
            t = _pendingSeekMs;
            ShowOsd($"Resumed at {Fmt(t)}");
            _pendingSeekMs = -1;
        }

        if (state is VLCState.Playing or VLCState.Paused && len > 0)
            SetMode(p.VideoTrackCount > 0 ? 0 : 2);

        if (len > 0 && !_seekDragging)
        {
            SeekSlider.ValueChanged -= SeekSlider_ValueChanged;
            SeekSlider.Value = (double)t / len * 1000.0;
            SeekSlider.ValueChanged += SeekSlider_ValueChanged;
        }
        CurrentTime.Text = Fmt(t);
        TotalTime.Text = len > 0 ? Fmt(len) : "--:--";
    }

    private void OnIdle()
    {
        _idleTimer.Stop();
        if (_isFullscreen && !ControlBar.IsMouseOver)
        {
            ControlBar.Visibility = Visibility.Collapsed;
            Overlay.Cursor = Cursors.None;
        }
    }

    private void ShowControls()
    {
        ControlBar.Visibility = Visibility.Visible;
        Overlay.Cursor = Cursors.Arrow;
        _idleTimer.Stop();
        if (_isFullscreen) _idleTimer.Start();
    }

    // ---------------------------------------------------------------- opening
    private void OpenPaths(IEnumerable<string> paths, bool append)
    {
        var files = new List<string>();
        foreach (var p in paths)
        {
            if (Directory.Exists(p))
                files.AddRange(Directory.EnumerateFiles(p).Where(IsMedia).OrderBy(f => f, StringComparer.OrdinalIgnoreCase));
            else if (p.Contains("://", StringComparison.Ordinal) || File.Exists(p))
                files.Add(p);
        }
        if (files.Count == 0) return;

        if (!append)
        {
            SavePosition();
            _playlist.Clear();
            _index = -1;
        }

        int first = _playlist.Count;
        foreach (var f in files) _playlist.Add(new PlaylistItem(f));

        if (!append || _index < 0) PlayIndex(first);
        else ShowOsd($"Added {files.Count} item(s)");
    }

    private void PlayIndex(int i)
    {
        if (_player == null || _libVLC == null || i < 0 || i >= _playlist.Count) return;

        SavePosition();
        foreach (var it in _playlist) it.IsCurrent = false;
        _index = i;
        var item = _playlist[i];
        item.IsCurrent = true;
        PlaylistList.ScrollIntoView(item);

        _currentKey = item.Path;
        _pendingSeekMs = _settings.Positions.TryGetValue(item.Path, out var pos) ? pos : -1;

        using (var media = item.IsUrl
                   ? new Media(_libVLC, new Uri(item.Path))
                   : new Media(_libVLC, item.Path, FromType.FromPath))
        {
            _player.Play(media);
        }
        _player.SetRate(_rate);

        TitleText.Text = $"{item.Title} — Tavrynexo Player";
        Title = TitleText.Text;
        AudioTitle.Text = item.Title;
        SetMode(0);
        SeekSlider.Value = 0;
        AddRecent(item.Path);
        UpdatePlayState();
    }

    private void AddRecent(string path)
    {
        _settings.Recent.RemoveAll(r => string.Equals(r, path, StringComparison.OrdinalIgnoreCase));
        _settings.Recent.Insert(0, path);
        if (_settings.Recent.Count > 10) _settings.Recent.RemoveRange(10, _settings.Recent.Count - 10);
    }

    private void SavePosition()
    {
        if (_player == null || _currentKey == null) return;
        try
        {
            long len = _player.Length, t = _player.Time;
            if (len > 60_000 && t > 10_000 && t < len - 15_000)
            {
                _settings.Positions[_currentKey] = t;
                while (_settings.Positions.Count > 200) _settings.Positions.Remove(_settings.Positions.Keys.First());
            }
            else _settings.Positions.Remove(_currentKey);
        }
        catch { /* ignore */ }
    }

    private int NextIndex(bool auto)
    {
        if (_playlist.Count == 0) return -1;
        if (_settings.Shuffle && _playlist.Count > 1)
        {
            int n;
            do n = _rng.Next(_playlist.Count); while (n == _index);
            return n;
        }
        int next = _index + 1;
        if (next >= _playlist.Count) return _settings.RepeatMode == 1 ? 0 : -1;
        return next;
    }

    private int PrevIndex()
    {
        if (_playlist.Count == 0) return -1;
        if (_index <= 0) return _settings.RepeatMode == 1 ? _playlist.Count - 1 : -1;
        return _index - 1;
    }

    private void OnEnded()
    {
        if (_currentKey != null) _settings.Positions.Remove(_currentKey);
        if (_settings.RepeatMode == 2) { PlayIndex(_index); return; }
        int n = NextIndex(true);
        if (n >= 0) PlayIndex(n); else StopPlayback();
    }

    private void StopPlayback()
    {
        SavePosition();
        _player?.Stop();
        _currentKey = null;
        _pendingSeekMs = -1;
        foreach (var it in _playlist) it.IsCurrent = false;
        SetMode(1);
        SeekSlider.Value = 0;
        CurrentTime.Text = "0:00";
        TotalTime.Text = "0:00";
        TitleText.Text = "Tavrynexo Player";
        Title = "Tavrynexo Player";
        UpdatePlayState();
    }

    private void TogglePause()
    {
        if (_player == null) return;
        if (_playlist.Count == 0) { OpenFile_Click(this, new RoutedEventArgs()); return; }

        var st = _player.State;
        if (st == VLCState.Playing) _player.SetPause(true);
        else if (st == VLCState.Paused) _player.Play();
        else PlayIndex(_index >= 0 ? _index : 0);
        UpdatePlayState();
    }

    private void SeekBy(long ms)
    {
        if (_player == null || !_player.IsSeekable || _player.Length <= 0) return;
        long t = Math.Clamp(_player.Time + ms, 0, _player.Length);
        _player.Time = t;
        ShowOsd($"{Fmt(t)} / {Fmt(_player.Length)}");
    }

    private void AdjustVolume(int delta)
    {
        VolumeSlider.Value = Math.Clamp(VolumeSlider.Value + delta, 0, 100);
        ShowOsd($"Volume {(int)VolumeSlider.Value}%");
    }

    private void SetSpeed(float rate)
    {
        _rate = Math.Clamp(rate, 0.25f, 4f);
        _player?.SetRate(_rate);
        SpeedButton.Content = _rate.ToString("0.0#", CultureInfo.InvariantCulture) + "×";
        ShowOsd($"Speed {SpeedButton.Content}");
    }

    private void StepSpeed(int dir)
    {
        int idx = Array.FindIndex(SpeedSteps, s => Math.Abs(s - _rate) < 0.01f);
        if (idx < 0) idx = 3;
        idx = Math.Clamp(idx + dir, 0, SpeedSteps.Length - 1);
        SetSpeed(SpeedSteps[idx]);
    }

    private void CycleRepeat()
    {
        _settings.RepeatMode = (_settings.RepeatMode + 1) % 3;
        ApplyRepeatIcon();
        ShowOsd(_settings.RepeatMode switch { 0 => "Repeat off", 1 => "Repeat all", _ => "Repeat one" });
    }

    private void ToggleShuffle()
    {
        _settings.Shuffle = !_settings.Shuffle;
        ShuffleButton.IsChecked = _settings.Shuffle;
        ShowOsd(_settings.Shuffle ? "Shuffle on" : "Shuffle off");
    }

    private void SetPlaylistVisible(bool visible)
    {
        PlaylistPanel.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
        PlaylistButton.IsChecked = visible;
    }

    // ------------------------------------------------------------- fullscreen
    private void ToggleFullscreen()
    {
        if (_isFullscreen) ExitFullscreen(); else EnterFullscreen();
    }

    private void EnterFullscreen()
    {
        _wasMaximized = WindowState == WindowState.Maximized;
        _restoreBounds = _wasMaximized ? RestoreBounds : new Rect(Left, Top, Width, Height);
        _isFullscreen = true;
        if (_wasMaximized) WindowState = WindowState.Normal;

        var chrome = WindowChrome.GetWindowChrome(this);
        chrome.CaptionHeight = 0;
        chrome.ResizeBorderThickness = new Thickness(0);
        TitleBar.Visibility = Visibility.Collapsed;
        Root.Margin = new Thickness(0);
        SetRoundedCorners(false);

        var hwnd = new WindowInteropHelper(this).Handle;
        var monitor = Native.MonitorFromWindow(hwnd, 2);
        var mi = new Native.MONITORINFO { cbSize = Marshal.SizeOf<Native.MONITORINFO>() };
        if (Native.GetMonitorInfo(monitor, ref mi))
        {
            Native.SetWindowPos(hwnd, new IntPtr(-1), mi.rcMonitor.Left, mi.rcMonitor.Top,
                mi.rcMonitor.Right - mi.rcMonitor.Left, mi.rcMonitor.Bottom - mi.rcMonitor.Top, 0x0040);
        }
        FullscreenButton.Content = "\uE73F";
        ShowControls();
    }

    private void ExitFullscreen()
    {
        _isFullscreen = false;
        _idleTimer.Stop();

        var chrome = WindowChrome.GetWindowChrome(this);
        chrome.CaptionHeight = 36;
        chrome.ResizeBorderThickness = new Thickness(6);
        TitleBar.Visibility = Visibility.Visible;
        ControlBar.Visibility = Visibility.Visible;
        Overlay.Cursor = Cursors.Arrow;
        SetRoundedCorners(true);

        var hwnd = new WindowInteropHelper(this).Handle;
        Native.SetWindowPos(hwnd, new IntPtr(-2), 0, 0, 0, 0, 0x0001 | 0x0002 | 0x0040);
        if (TopmostMenuItem.IsChecked) { Topmost = false; Topmost = true; }

        Left = _restoreBounds.Left; Top = _restoreBounds.Top;
        Width = _restoreBounds.Width; Height = _restoreBounds.Height;
        if (_wasMaximized) WindowState = WindowState.Maximized;
        FullscreenButton.Content = "\uE740";
        UpdateWindowState();
    }

    private void SetRoundedCorners(bool round)
    {
        try
        {
            int pref = round ? 2 : 1; // DWMWCP_ROUND / DWMWCP_DONOTROUND (Windows 11)
            Native.DwmSetWindowAttribute(new WindowInteropHelper(this).Handle, 33, ref pref, sizeof(int));
        }
        catch { /* Windows 10 ignores this */ }
    }

    // --------------------------------------------------------------- keyboard
    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (Keyboard.FocusedElement is TextBox) return;

        bool ctrl = (Keyboard.Modifiers & ModifierKeys.Control) != 0;
        bool shift = (Keyboard.Modifiers & ModifierKeys.Shift) != 0;
        bool inList = Keyboard.FocusedElement is ListBox or ListBoxItem;
        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        bool handled = true;

        switch (key)
        {
            case Key.Space: TogglePause(); break;
            case Key.Left: SeekBy(ctrl ? -60_000 : shift ? -30_000 : -5_000); break;
            case Key.Right: SeekBy(ctrl ? 60_000 : shift ? 30_000 : 5_000); break;
            case Key.Up when !inList: AdjustVolume(5); break;
            case Key.Down when !inList: AdjustVolume(-5); break;
            case Key.M: Mute_Click(this, new RoutedEventArgs()); break;
            case Key.F:
            case Key.F11: ToggleFullscreen(); break;
            case Key.Escape when _isFullscreen: ExitFullscreen(); break;
            case Key.N: Next_Click(this, new RoutedEventArgs()); break;
            case Key.P: Prev_Click(this, new RoutedEventArgs()); break;
            case Key.R: CycleRepeat(); break;
            case Key.H: ToggleShuffle(); break;
            case Key.OemOpenBrackets: StepSpeed(-1); break;
            case Key.OemCloseBrackets: StepSpeed(1); break;
            case Key.Back: SetSpeed(1f); break;
            case Key.O when ctrl && shift: AddFiles_Click(this, new RoutedEventArgs()); break;
            case Key.O when ctrl: OpenFile_Click(this, new RoutedEventArgs()); break;
            case Key.U when ctrl: OpenUrl_Click(this, new RoutedEventArgs()); break;
            case Key.L when ctrl: Playlist_Click(this, new RoutedEventArgs()); break;
            case Key.S when ctrl: Snapshot_Click(this, new RoutedEventArgs()); break;
            case Key.Delete when inList: RemoveSelectedPlaylistItem(); break;
            default: handled = false; break;
        }
        if (handled) e.Handled = true;
    }

    private void RemoveSelectedPlaylistItem()
    {
        int i = PlaylistList.SelectedIndex;
        if (i < 0) return;
        _playlist.RemoveAt(i);
        if (i < _index) _index--;
        else if (i == _index) _index = i - 1;
    }

    // ---------------------------------------------------------- drag and drop
    private void Window_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    private void Window_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetData(DataFormats.FileDrop) is string[] files && files.Length > 0)
            OpenPaths(files, append: (Keyboard.Modifiers & ModifierKeys.Shift) != 0);
        e.Handled = true;
    }

    // ------------------------------------------------------------ overlay mouse
    private void Overlay_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            _clickTimer.Stop();
            ToggleFullscreen();
        }
        else
        {
            _clickTimer.Stop();
            _clickTimer.Start();
        }
        Dispatcher.BeginInvoke(new Action(() => Activate()));
    }

    private void Overlay_MouseMove(object sender, MouseEventArgs e)
    {
        var pos = e.GetPosition(Overlay);
        if (Math.Abs(pos.X - _lastMouse.X) < 3 && Math.Abs(pos.Y - _lastMouse.Y) < 3) return;
        _lastMouse = pos;
        ShowControls();
    }

    private void Overlay_MouseWheel(object sender, MouseWheelEventArgs e) => AdjustVolume(e.Delta > 0 ? 5 : -5);

    // ------------------------------------------------------- title bar buttons
    private void Minimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

    private void Maximize_Click(object sender, RoutedEventArgs e) =>
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
    private void Exit_Click(object sender, RoutedEventArgs e) => Close();

    // ------------------------------------------------------------------ media
    private void OpenFile_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Title = "Open media",
            Multiselect = true,
            Filter = "Media files|" + string.Join(";", MediaExtensions.Select(x => "*" + x)) + "|All files|*.*"
        };
        if (dlg.ShowDialog(this) == true) OpenPaths(dlg.FileNames, append: false);
    }

    private void AddFiles_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Title = "Add to playlist",
            Multiselect = true,
            Filter = "Media files|" + string.Join(";", MediaExtensions.Select(x => "*" + x)) + "|All files|*.*"
        };
        if (dlg.ShowDialog(this) == true) OpenPaths(dlg.FileNames, append: true);
    }

    private void OpenFolder_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFolderDialog { Title = "Open folder" };
        if (dlg.ShowDialog(this) == true) OpenPaths(new[] { dlg.FolderName }, append: false);
    }

    private void OpenUrl_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new UrlDialog { Owner = this };
        if (dlg.ShowDialog() == true && dlg.Url.Length > 0) OpenPaths(new[] { dlg.Url }, append: false);
    }

    private void RecentMenu_Opened(object sender, RoutedEventArgs e)
    {
        if (!ReferenceEquals(e.OriginalSource, RecentMenu)) return;
        RecentMenu.Items.Clear();
        if (_settings.Recent.Count == 0)
        {
            RecentMenu.Items.Add(new MenuItem { Header = "(empty)", IsEnabled = false });
            return;
        }
        foreach (var path in _settings.Recent.ToList())
        {
            var mi = new MenuItem
            {
                Header = (path.Length > 60 ? "…" + path[^58..] : path).Replace("_", "__")
            };
            var captured = path;
            mi.Click += (_, _) => OpenPaths(new[] { captured }, append: false);
            RecentMenu.Items.Add(mi);
        }
        RecentMenu.Items.Add(new Separator());
        var clear = new MenuItem { Header = "Clear recent" };
        clear.Click += (_, _) => _settings.Recent.Clear();
        RecentMenu.Items.Add(clear);
    }

    // --------------------------------------------------------------- playback
    private void PlayPause_Click(object sender, RoutedEventArgs e) => TogglePause();
    private void Stop_Click(object sender, RoutedEventArgs e) => StopPlayback();

    private void Next_Click(object sender, RoutedEventArgs e)
    {
        int n = NextIndex(false);
        if (n >= 0) PlayIndex(n);
    }

    private void Prev_Click(object sender, RoutedEventArgs e)
    {
        if (_player != null && _player.Time > 3000) { _player.Time = 0; return; }
        int p = PrevIndex();
        if (p >= 0) PlayIndex(p);
        else if (_player != null) _player.Time = 0;
    }

    private void Forward10_Click(object sender, RoutedEventArgs e) => SeekBy(10_000);
    private void Back10_Click(object sender, RoutedEventArgs e) => SeekBy(-10_000);

    private void Speed_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem mi && mi.Tag is string tag &&
            float.TryParse(tag, NumberStyles.Float, CultureInfo.InvariantCulture, out var r))
            SetSpeed(r);
    }

    private void SpeedButton_Click(object sender, RoutedEventArgs e) => SetSpeed(1f);
    private void Repeat_Click(object sender, RoutedEventArgs e) => CycleRepeat();
    private void Shuffle_Click(object sender, RoutedEventArgs e) => ToggleShuffle();
    private void RepeatButton_Click(object sender, RoutedEventArgs e) => CycleRepeat();
    private void ShuffleButton_Click(object sender, RoutedEventArgs e) { _settings.Shuffle = ShuffleButton.IsChecked == true; }

    private void SeekSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        var p = _player;
        if (p == null || p.Length <= 0 || !p.IsSeekable) return;
        p.Position = (float)(e.NewValue / 1000.0);
        CurrentTime.Text = Fmt((long)(e.NewValue / 1000.0 * p.Length));
    }

    // ------------------------------------------------------------------ audio
    private void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_player == null) return;
        int v = (int)Math.Round(e.NewValue);
        _player.Volume = v;
        _settings.Volume = v;
        VolumeText.Text = $"{v}%";
        if (v > 0 && _player.Mute) _player.Mute = false;
        UpdateMuteIcon();
    }

    private void VolUp_Click(object sender, RoutedEventArgs e) => AdjustVolume(5);
    private void VolDown_Click(object sender, RoutedEventArgs e) => AdjustVolume(-5);

    private void Mute_Click(object sender, RoutedEventArgs e)
    {
        if (_player == null) return;
        _player.Mute = !_player.Mute;
        _settings.Muted = _player.Mute;
        UpdateMuteIcon();
        ShowOsd(_player.Mute ? "Muted" : $"Volume {(int)VolumeSlider.Value}%");
    }

    private void AudioDelayMinus_Click(object sender, RoutedEventArgs e) => ChangeAudioDelay(-50_000);
    private void AudioDelayPlus_Click(object sender, RoutedEventArgs e) => ChangeAudioDelay(50_000);

    private void ChangeAudioDelay(long micro)
    {
        if (_player == null) return;
        long d = _player.AudioDelay + micro;
        _player.SetAudioDelay(d);
        ShowOsd($"Audio delay {d / 1000} ms");
    }

    private void AudioTrackMenu_Opened(object sender, RoutedEventArgs e)
    {
        if (!ReferenceEquals(e.OriginalSource, AudioTrackMenu)) return;
        AudioTrackMenu.Items.Clear();
        var p = _player;
        var tracks = p?.AudioTrackDescription ?? Array.Empty<TrackDescription>();
        if (p == null || tracks.Length == 0)
        {
            AudioTrackMenu.Items.Add(new MenuItem { Header = "(none)", IsEnabled = false });
            return;
        }
        int current = p.AudioTrack;
        foreach (var t in tracks)
        {
            var id = t.Id;
            var mi = new MenuItem { Header = t.Name.Replace("_", "__"), IsCheckable = true, IsChecked = id == current };
            mi.Click += (_, _) => { _player?.SetAudioTrack(id); ShowOsd("Audio track changed"); };
            AudioTrackMenu.Items.Add(mi);
        }
    }

    // ------------------------------------------------------------------ video
    private void Aspect_Click(object sender, RoutedEventArgs e)
    {
        if (_player == null || sender is not MenuItem mi) return;
        var tag = mi.Tag as string;
        _player.AspectRatio = string.IsNullOrEmpty(tag) ? null : tag;
        ShowOsd(string.IsNullOrEmpty(tag) ? "Aspect ratio: default" : $"Aspect ratio {tag}");
    }

    private void Fullscreen_Click(object sender, RoutedEventArgs e) => ToggleFullscreen();

    private void Snapshot_Click(object sender, RoutedEventArgs e)
    {
        if (_player == null || _mode != 0) return;
        try
        {
            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "Tavrynexo");
            Directory.CreateDirectory(dir);
            var file = Path.Combine(dir, $"Tavrynexo_{DateTime.Now:yyyyMMdd_HHmmss}.png");
            ShowOsd(_player.TakeSnapshot(0, file, 0, 0) ? "Snapshot saved to Pictures\\Tavrynexo" : "Snapshot failed");
        }
        catch { ShowOsd("Snapshot failed"); }
    }

    // --------------------------------------------------------------- subtitles
    private void AddSubtitle_Click(object sender, RoutedEventArgs e)
    {
        if (_player == null) return;
        var dlg = new OpenFileDialog
        {
            Title = "Add subtitle",
            Filter = "Subtitles|*.srt;*.ass;*.ssa;*.sub;*.vtt|All files|*.*"
        };
        if (dlg.ShowDialog(this) == true)
        {
            _player.AddSlave(MediaSlaveType.Subtitle, new Uri(dlg.FileName).AbsoluteUri, true);
            ShowOsd("Subtitle loaded");
        }
    }

    private void SubtitleTrackMenu_Opened(object sender, RoutedEventArgs e)
    {
        if (!ReferenceEquals(e.OriginalSource, SubtitleTrackMenu)) return;
        SubtitleTrackMenu.Items.Clear();
        var p = _player;
        var tracks = p?.SpuDescription ?? Array.Empty<TrackDescription>();
        if (p == null || tracks.Length == 0)
        {
            SubtitleTrackMenu.Items.Add(new MenuItem { Header = "(none)", IsEnabled = false });
            return;
        }
        int current = p.Spu;
        foreach (var t in tracks)
        {
            var id = t.Id;
            var mi = new MenuItem { Header = t.Name.Replace("_", "__"), IsCheckable = true, IsChecked = id == current };
            mi.Click += (_, _) => { _player?.SetSpu(id); ShowOsd("Subtitle track changed"); };
            SubtitleTrackMenu.Items.Add(mi);
        }
    }

    private void SubDelayMinus_Click(object sender, RoutedEventArgs e) => ChangeSubDelay(-50_000);
    private void SubDelayPlus_Click(object sender, RoutedEventArgs e) => ChangeSubDelay(50_000);

    private void ChangeSubDelay(long micro)
    {
        if (_player == null) return;
        long d = _player.SpuDelay + micro;
        _player.SetSpuDelay(d);
        ShowOsd($"Subtitle delay {d / 1000} ms");
    }

    // -------------------------------------------------------------- view/help
    private void Playlist_Click(object sender, RoutedEventArgs e)
    {
        SetPlaylistVisible(PlaylistPanel.Visibility != Visibility.Visible);
    }

    private void Topmost_Click(object sender, RoutedEventArgs e) => Topmost = TopmostMenuItem.IsChecked;

    private void ClearPlaylist_Click(object sender, RoutedEventArgs e)
    {
        StopPlayback();
        _playlist.Clear();
        _index = -1;
    }

    private void PlaylistList_DoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is DependencyObject d && ItemsControl.ContainerFromElement(PlaylistList, d) is ListBoxItem)
        {
            int i = PlaylistList.SelectedIndex;
            if (i >= 0) PlayIndex(i);
        }
    }

    private void Shortcuts_Click(object sender, RoutedEventArgs e)
    {
        const string body =
            "Space\t\tPlay / Pause\n" +
            "← / →\t\tSeek 5 s  (Shift 30 s, Ctrl 60 s)\n" +
            "↑ / ↓\t\tVolume\n" +
            "M\t\tMute\n" +
            "F / F11\t\tFullscreen  (Esc to exit)\n" +
            "N / P\t\tNext / Previous\n" +
            "[  ]\t\tSlower / Faster  (Backspace resets)\n" +
            "R / H\t\tRepeat / Shuffle\n" +
            "Ctrl+O\t\tOpen file\n" +
            "Ctrl+Shift+O\tAdd to playlist\n" +
            "Ctrl+U\t\tOpen network stream\n" +
            "Ctrl+L\t\tPlaylist\n" +
            "Ctrl+S\t\tSnapshot";
        new InfoDialog("Keyboard Shortcuts", "Tavrynexo Player", body) { Owner = this }.ShowDialog();
    }

    private void About_Click(object sender, RoutedEventArgs e)
    {
        var v = typeof(MainWindow).Assembly.GetName().Version;
        new InfoDialog("Tavrynexo Player", $"Version {v?.Major}.{v?.Minor}.{v?.Build}",
            "A modern, fast media player for Windows 10 and Windows 11.\n\n" +
            "Playback engine: LibVLC (VideoLAN), licensed under LGPL 2.1+.\n" +
            "© Tavrynexo. All rights reserved.") { Owner = this }.ShowDialog();
    }

    // ------------------------------------------------------------------ native
    private static class Native
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct RECT { public int Left, Top, Right, Bottom; }

        [StructLayout(LayoutKind.Sequential)]
        public struct MONITORINFO { public int cbSize; public RECT rcMonitor; public RECT rcWork; public uint dwFlags; }

        [DllImport("user32.dll")] public static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint flags);
        [DllImport("user32.dll")] public static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);
        [DllImport("user32.dll")]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr after, int x, int y, int cx, int cy, uint flags);
        [DllImport("dwmapi.dll")]
        public static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);
    }
}
