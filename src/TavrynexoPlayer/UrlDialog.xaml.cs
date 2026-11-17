using System.Windows;
using System.Windows.Input;

namespace TavrynexoPlayer;

public partial class UrlDialog : Window
{
    public UrlDialog()
    {
        InitializeComponent();
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
