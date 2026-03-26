using System.Windows;
using System.Windows.Controls;
using BIMConcierge.UI.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace BIMConcierge.UI.Views.Sections;

public partial class TutorialDetailSectionView : UserControl
{
    private readonly TutorialViewModel _vm;

    public TutorialDetailSectionView()
    {
        InitializeComponent();
        _vm = ServiceLocator.ServiceProvider!
            .GetRequiredService<TutorialViewModel>();
        DataContext = _vm;
    }

    public void InitializeTutorial(string tutorialId)
    {
        _ = _vm.LoadTutorialAsync(tutorialId);
    }
}
