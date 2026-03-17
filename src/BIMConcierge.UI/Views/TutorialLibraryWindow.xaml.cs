using System.Windows;
using System.Windows.Input;

namespace BIMConcierge.UI.Views;

public partial class TutorialLibraryWindow : Window
{
    public TutorialLibraryWindow()
    {
        InitializeComponent();
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
