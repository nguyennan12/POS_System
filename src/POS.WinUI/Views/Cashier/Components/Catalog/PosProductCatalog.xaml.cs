using System.Windows;
using System.Windows.Controls;
using POS.WinUI.ViewModels.Cashier;

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

    private void OnCatalogScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        // Khi người dùng cuộn dọc hoặc kích thước vùng hiển thị thay đổi
        if (e.VerticalChange > 0 || e.ExtentHeightChange > 0)
        {
            if (sender is ScrollViewer scrollViewer)
            {
                // Tải trang tiếp theo từ BE khi cuộn gần tới đáy (còn cách đáy dưới 100px)
                if (scrollViewer.ScrollableHeight > 0 &&
                    scrollViewer.VerticalOffset >= scrollViewer.ScrollableHeight - 100)
                {
                    if (DataContext is PosCashierMainViewModel vm &&
                        vm.LoadMoreCatalogCommand.CanExecute(null))
                    {
                        vm.LoadMoreCatalogCommand.Execute(null);
                    }
                }
            }
        }
    }
}

