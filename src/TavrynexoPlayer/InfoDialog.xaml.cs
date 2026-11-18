using System.Windows;
using System.Windows.Input;
using TavrynexoPlayer.Services;

namespace TavrynexoPlayer;

public partial class InfoDialog : Window
{
    public InfoDialog(string heading, string subtitle, string body)
    {
        InitializeComponent();
        FlowDirection = Loc.Instance.IsRtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
        CloseBtn.Content = Loc.T("btn_close");
        HeadingText.Text = heading;
        SubText.Text = subtitle;
        BodyText.Text = body;
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    private void Header_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed) DragMove();
    }
}
