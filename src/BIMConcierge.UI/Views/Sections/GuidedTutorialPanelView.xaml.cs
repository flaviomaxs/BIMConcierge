using System.Windows;
using System.Windows.Controls;
using BIMConcierge.UI.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace BIMConcierge.UI.Views.Sections;

public partial class GuidedTutorialPanelView : UserControl
{
    private readonly TutorialViewModel _vm;

    public event Action? CloseRequested;

    public GuidedTutorialPanelView()
    {
        InitializeComponent();
        _vm = ServiceLocator.ServiceProvider!
            .GetRequiredService<TutorialViewModel>();
        DataContext = _vm;
    }

    public async void InitializeTutorial(string tutorialId)
    {
        await _vm.LoadTutorialAsync(tutorialId);
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e)
    {
        CloseRequested?.Invoke();
    }
}
