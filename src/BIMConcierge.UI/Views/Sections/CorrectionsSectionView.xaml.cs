using System.Windows;
using System.Windows.Controls;
using BIMConcierge.UI.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace BIMConcierge.UI.Views.Sections;

public partial class CorrectionsSectionView : UserControl
{
    public CorrectionsSectionView()
    {
        InitializeComponent();
        DataContext = ServiceLocator.ServiceProvider!
            .GetRequiredService<CorrectionViewModel>();
        Loaded += (_, _) => (DataContext as CorrectionViewModel)?.LoadCommand.Execute(null);
    }
}
