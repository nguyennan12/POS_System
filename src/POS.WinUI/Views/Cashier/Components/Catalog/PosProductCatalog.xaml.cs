using System.Windows;
using System.Windows.Controls;

namespace POS.WinUI.Views.Cashier.Components;

public partial class PosProductCatalog : UserControl
{
    public event RoutedEventHandler? ProductCardClicked;

    public PosProductCatalog()
    {
        InitializeComponent();
    }

    private void OnCardClick(object sender, RoutedEventArgs e)
    {
        ProductCardClicked?.Invoke(sender, e);
    }
}
