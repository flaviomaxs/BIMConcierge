using System.Windows;
using System.Windows.Controls;
using BIMConcierge.UI.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace BIMConcierge.UI.Views.Sections;

public partial class AchievementsSectionView : UserControl
{
    public AchievementsSectionView()
    {
        InitializeComponent();
        DataContext = ServiceLocator.ServiceProvider!
            .GetRequiredService<AchievementsViewModel>();
        Loaded += (_, _) => (DataContext as AchievementsViewModel)?.LoadCommand.Execute(null);
    }
}
