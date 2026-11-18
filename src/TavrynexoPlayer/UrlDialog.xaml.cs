using System.Windows;
using System.Windows.Input;
using TavrynexoPlayer.Services;

namespace TavrynexoPlayer;

public partial class UrlDialog : Window
{
    public UrlDialog()
    {
        InitializeComponent();
        FlowDirection = Loc.Instance.IsRtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
        Title = TitleText.Text = Loc.Plain("open_url");
        HintText.Text = Loc.T("url_hint");
        CancelBtn.Content = Loc.T("btn_cancel");
        PlayBtn.Content = Loc.T("btn_play");
        try
        {
            if (Clipboard.ContainsText())
            {
                var text = Clipboard.GetText().Trim();
                if (text.Contains("://", StringComparison.Ordinal) && text.Length < 2000) UrlBox.Text = text;
            }
        }
        catch { /* clipboard may be locked */ }
        Loaded += (_, _) => { UrlBox.Focus(); UrlBox.SelectAll(); };
    }

    public string Url => UrlBox.Text.Trim();

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        if (Url.Length == 0) return;
        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;

    private void Header_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed && e.OriginalSource is not System.Windows.Controls.TextBox) DragMove();
    }
}
