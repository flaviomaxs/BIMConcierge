using System.Windows;
using System.Windows.Controls;
using BIMConcierge.UI.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace BIMConcierge.UI.Views.Sections;

public partial class ProgressSectionView : UserControl
{
    public ProgressSectionView()
    {
        InitializeComponent();
        DataContext = ServiceLocator.ServiceProvider!
            .GetRequiredService<StudentProgressViewModel>();
        Loaded += (_, _) => (DataContext as StudentProgressViewModel)?.LoadCommand.Execute(null);
    }
}
