using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using BIMConcierge.UI.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace BIMConcierge.UI.Views.Sections;

public partial class SettingsSectionView : UserControl
{
    private readonly SettingsViewModel _vm;

    public SettingsSectionView()
    {
        InitializeComponent();
        _vm = ServiceLocator.ServiceProvider!
            .GetRequiredService<SettingsViewModel>();
        DataContext = _vm;

        Loaded += (_, _) => _vm.LoadCommand.Execute(null);
    }

    private void LangPtBr_Click(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
        _vm.ChangeLanguageCommand.Execute("pt-BR");
    }

    private void LangEn_Click(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
        _vm.ChangeLanguageCommand.Execute("en");
    }
}
