using System;
using System.Windows;
using System.Windows.Input;
// using BIMConcierge.UI.ViewModels; // Descomente quando criar o ViewModel desta tela

namespace BIMConcierge.UI.Views;

public partial class CorrectionAlertWindow : Window
{
    public CorrectionAlertWindow()
    {
        InitializeComponent();

        // Lógica para posicionar a notificação no canto INFERIOR DIREITO da tela
        this.Loaded += (s, e) =>
        {
            var desktopWorkingArea = SystemParameters.WorkArea;
            this.Left = desktopWorkingArea.Right - this.Width - 20;
            this.Top = desktopWorkingArea.Bottom - this.Height - 20;
        };
    }

    // Permite que o usuário arraste o alerta caso ele esteja cobrindo algo importante no Revit
    private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
        {
            this.DragMove();
        }
    }

    // Fecha o alerta (usado tanto no "X" quanto no botão "Ignorar")
    private void BtnClose_Click(object sender, RoutedEventArgs e)
    {
        this.Close();
    }
}
