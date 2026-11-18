using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TavrynexoPlayer.Services;

namespace TavrynexoPlayer;

public partial class LanguageDialog : Window
{
    public LanguageDialog()
    {
        InitializeComponent();
        FlowDirection = Loc.Instance.IsRtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
        Title = TitleText.Text = Loc.Plain("language");
        CancelBtn.Content = Loc.T("btn_cancel");
        OkBtn.Content = Loc.T("btn_ok");
        SearchBox.ToolTip = Loc.T("lang_search");
        FillList();
        Loaded += (_, _) => SearchBox.Focus();
    }

    public string? SelectedCode { get; private set; }

    private void FillList()
    {
        var filter = SearchBox.Text.Trim();
        var items = Loc.Languages
            .Where(l => filter.Length == 0 || l.Display.Contains(filter, StringComparison.CurrentCultureIgnoreCase)
                                           || l.Code.Contains(filter, StringComparison.OrdinalIgnoreCase))
            .ToList();
        LangList.ItemsSource = items;
        var current = items.FirstOrDefault(l => l.Code == Loc.Instance.Code);
        if (current != null)
        {
            LangList.SelectedItem = current;
            LangList.ScrollIntoView(current);
        }
        else if (items.Count > 0) LangList.SelectedIndex = 0;
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (LangList != null) FillList();
    }

    private void Commit()
    {
        if (LangList.SelectedItem is LanguageInfo li)
        {
            SelectedCode = li.Code;
            DialogResult = true;
        }
    }

    private void Ok_Click(object sender, RoutedEventArgs e) => Commit();
    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;

    private void LangList_DoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is DependencyObject d && ItemsControl.ContainerFromElement(LangList, d) is ListBoxItem) Commit();
    }

    private void Header_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed && e.OriginalSource is not TextBox && e.OriginalSource is not ScrollViewer) DragMove();
    }
}
