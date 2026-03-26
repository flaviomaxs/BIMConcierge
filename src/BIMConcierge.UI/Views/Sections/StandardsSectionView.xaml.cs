using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using BIMConcierge.UI.Localization;
using BIMConcierge.UI.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;

namespace BIMConcierge.UI.Views.Sections;

public partial class StandardsSectionView : UserControl
{
    private static readonly JsonSerializerOptions s_jsonOptions = new() { WriteIndented = true };

    public StandardsSectionView()
    {
        InitializeComponent();
        DataContext = ServiceLocator.ServiceProvider!
            .GetRequiredService<CompanyStandardsViewModel>();
        Loaded += (_, _) => (DataContext as CompanyStandardsViewModel)?.LoadCommand.Execute(null);
    }

    private CompanyStandardsViewModel Vm => (CompanyStandardsViewModel)DataContext;

    private void CategoryNaming_Click(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
        Vm.SelectCategoryCommand.Execute("Naming Conventions");
    }

    private void CategoryLOD_Click(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
        Vm.SelectCategoryCommand.Execute("LOD Requirements");
    }

    private void CategoryWorkset_Click(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
        Vm.SelectCategoryCommand.Execute("Workset Rules");
    }

    private void CategoryBestPractices_Click(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
        Vm.SelectCategoryCommand.Execute("Best Practices");
    }

    private void BtnExportJson_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Filter = "JSON|*.json",
            FileName = "bimconcierge-standards.json"
        };

        if (dialog.ShowDialog() == true)
        {
            var json = JsonSerializer.Serialize(Vm.Standards, s_jsonOptions);
            File.WriteAllText(dialog.FileName, json);
            MessageBox.Show(
                TranslationSource.GetString("StandardsExportSuccess"),
                TranslationSource.GetString("StandardsExportJson"),
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
    }

    private void AddCategory_Click(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
        MessageBox.Show(
            TranslationSource.GetString("StandardsAddCategoryMessage"),
            TranslationSource.GetString("StandardsAddCategory"),
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }
}
