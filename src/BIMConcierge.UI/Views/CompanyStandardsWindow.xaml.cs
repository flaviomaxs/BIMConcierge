using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using BIMConcierge.UI.Localization;
using BIMConcierge.UI.ViewModels;
using Microsoft.Win32;

namespace BIMConcierge.UI.Views;

public partial class CompanyStandardsWindow : Window
{
    private static readonly JsonSerializerOptions s_jsonOptions = new() { WriteIndented = true };
    private readonly CompanyStandardsViewModel _vm;

    public CompanyStandardsWindow(CompanyStandardsViewModel viewModel)
    {
        InitializeComponent();
        _vm = viewModel;
        DataContext = viewModel;
        Loaded += (_, _) => viewModel.LoadCommand.Execute(null);
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
        _vm.OpenWindowCommand.Execute("Dashboard");
        this.Close();
    }

    private void SidebarProgress_Click(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
        _vm.OpenWindowCommand.Execute("StudentProgress");
        this.Close();
    }

    private void SidebarAchievements_Click(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
        _vm.OpenWindowCommand.Execute("Achievements");
        this.Close();
    }

    private void CategoryNaming_Click(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
        _vm.SelectCategoryCommand.Execute("Naming Conventions");
    }

    private void CategoryLOD_Click(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
        _vm.SelectCategoryCommand.Execute("LOD Requirements");
    }

    private void CategoryWorkset_Click(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
        _vm.SelectCategoryCommand.Execute("Workset Rules");
    }

    private void CategoryBestPractices_Click(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
        _vm.SelectCategoryCommand.Execute("Best Practices");
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
            var json = JsonSerializer.Serialize(_vm.Standards, s_jsonOptions);
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
