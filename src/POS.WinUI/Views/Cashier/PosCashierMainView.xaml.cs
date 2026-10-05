using System.Windows;
using System.Windows.Controls;
using POS.WinUI.ViewModels.Cashier;

namespace POS.WinUI.Views.Cashier;

public partial class PosCashierMainView : UserControl
{
    public PosCashierMainView(PosCashierMainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        Loaded += (_, _) => Focus();
    }

    /// <summary>
    /// Khi bấm thêm sản phẩm, kích hoạt hiệu ứng nảy nhẹ biểu tượng giỏ hàng phản hồi
    /// </summary>
    private void OnProductCardClick(object sender, RoutedEventArgs e)
    {
        CartView?.PlayBounceAnimation();
    }
}
