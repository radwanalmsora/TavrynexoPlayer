using System.ComponentModel;
using System.IO;

namespace TavrynexoPlayer.Models;

public sealed class PlaylistItem : INotifyPropertyChanged
{
    private bool _isCurrent;

    public PlaylistItem(string path)
    {
        Path = path;
        IsUrl = path.Contains("://", StringComparison.Ordinal);
        if (IsUrl)
        {
            Title = Uri.TryCreate(path, UriKind.Absolute, out var uri)
                ? (string.IsNullOrEmpty(uri.Segments.LastOrDefault()?.Trim('/')) ? uri.Host : Uri.UnescapeDataString(uri.Segments.Last().Trim('/')))
                : path;
        }
        else
        {
            Title = System.IO.Path.GetFileNameWithoutExtension(path);
        }
    }

    public string Path { get; }
    public string Title { get; }
    public bool IsUrl { get; }

    public bool IsCurrent
    {
        get => _isCurrent;
        set { if (_isCurrent != value) { _isCurrent = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsCurrent))); } }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}
