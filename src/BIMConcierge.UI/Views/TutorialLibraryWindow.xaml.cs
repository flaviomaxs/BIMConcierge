using System.Windows;
using System.Windows.Input;
using BIMConcierge.UI.ViewModels;

namespace BIMConcierge.UI.Views;

public partial class TutorialLibraryWindow : Window
{
    public TutorialLibraryWindow(TutorialLibraryViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        Loaded += (_, _) => viewModel.LoadCommand.Execute(null);
    }

    // Permite que o usuário arraste a janela clicando no fundo escuro
    private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
        {
            this.DragMove();
        }
    }

    // Evento do botão ✕ no canto superior direito
    private void BtnClose_Click(object sender, RoutedEventArgs e)
    {
        this.Close();
    }
}
