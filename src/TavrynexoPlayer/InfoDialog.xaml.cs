using System.Windows;
using System.Windows.Input;

namespace TavrynexoPlayer;

public partial class InfoDialog : Window
{
    public InfoDialog(string heading, string subtitle, string body)
    {
        InitializeComponent();
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
