using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Navigation;
using TavrynexoPlayer.Services;

namespace TavrynexoPlayer;

public partial class AboutDialog : Window
{
    public AboutDialog()
    {
        InitializeComponent();
        FlowDirection = Loc.Instance.IsRtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
        var v = typeof(AboutDialog).Assembly.GetName().Version;
        VersionText.Text = Loc.F("about_version", $"{v?.Major}.{v?.Minor}.{v?.Build}");
        DescText.Text = Loc.T("about_desc");
        DevLabel.Text = Loc.T("about_developer");
        MailLabel.Text = Loc.T("about_email");
        SrcLabel.Text = Loc.T("about_source");
        EngineText.Text = Loc.T("about_engine");
        CloseBtn.Content = Loc.T("btn_close");
    }

    public static void OpenLink(string url)
    {
        try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); }
        catch { /* no default handler */ }
    }

    private void Link_Navigate(object sender, RequestNavigateEventArgs e)
    {
        OpenLink(e.Uri.AbsoluteUri);
        e.Handled = true;
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    private void Header_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed) DragMove();
    }
}
