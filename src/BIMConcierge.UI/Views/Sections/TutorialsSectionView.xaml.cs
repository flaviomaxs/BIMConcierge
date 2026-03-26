using System.Windows;
using System.Windows.Controls;
using BIMConcierge.UI.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace BIMConcierge.UI.Views.Sections;

public partial class TutorialsSectionView : UserControl
{
    public TutorialsSectionView()
    {
        InitializeComponent();
        DataContext = ServiceLocator.ServiceProvider!
            .GetRequiredService<TutorialLibraryViewModel>();
        Loaded += (_, _) => (DataContext as TutorialLibraryViewModel)?.LoadCommand.Execute(null);
    }
}
