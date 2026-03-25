using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using BIMConcierge.UI.Localization;
using BIMConcierge.UI.ViewModels;

namespace BIMConcierge.UI.Views;

public partial class DashboardWindow : Window
{
    private readonly DashboardViewModel _vm;

    public DashboardWindow(DashboardViewModel viewModel)
    {
        InitializeComponent();
        _vm = viewModel;
        DataContext = viewModel;
        UpdateLanguageButtons();
        Loaded += (_, _) => _vm.LoadCommand.Execute(null);
    }

    private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
            this.DragMove();
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e) => this.Close();

    private void SidebarDashboard_Click(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
        _vm.NavigateToCommand.Execute("Dashboard");
    }

    private void SidebarTutorials_Click(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
        _vm.NavigateToCommand.Execute("Tutorials");
    }

    private void SidebarCorrections_Click(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
        _vm.NavigateToCommand.Execute("Corrections");
    }

    private void SidebarStandards_Click(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
        _vm.NavigateToCommand.Execute("Standards");
    }

    private void SidebarProgress_Click(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
        _vm.NavigateToCommand.Execute("Progress");
    }

    private void SidebarAchievements_Click(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
        _vm.NavigateToCommand.Execute("Achievements");
    }

    private void BtnViewDetails_Click(object sender, RoutedEventArgs e) =>
        _vm.NavigateToCommand.Execute("Standards");

    private void BtnLangPt_Click(object sender, RoutedEventArgs e)
    {
        TranslationSource.Instance.SetCulture("pt-BR");
        UpdateLanguageButtons();
    }

    private void BtnLangEn_Click(object sender, RoutedEventArgs e)
    {
        TranslationSource.Instance.SetCulture("en");
        UpdateLanguageButtons();
    }

    private void UpdateLanguageButtons()
    {
        bool isPt = TranslationSource.Instance.CurrentCulture.Name.StartsWith("pt", StringComparison.OrdinalIgnoreCase);
        var activeBg = new BrushConverter().ConvertFrom("#6A7D90") as Brush;
        var inactiveBg = Brushes.Transparent;
        var activeFg = Brushes.White;
        var inactiveFg = new BrushConverter().ConvertFrom("#94A3B8") as Brush;

        BtnLangPt.Background = isPt ? activeBg! : inactiveBg;
        BtnLangPt.Foreground = isPt ? activeFg : inactiveFg!;
        BtnLangEn.Background = isPt ? inactiveBg : activeBg!;
        BtnLangEn.Foreground = isPt ? inactiveFg! : activeFg;
    }
}
