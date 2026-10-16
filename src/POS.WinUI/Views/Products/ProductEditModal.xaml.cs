using System.Windows;
using System.Windows.Controls;

namespace POS.WinUI.Views.Products;

public partial class ProductEditModal : UserControl
{
    public ProductEditModal()
    {
        InitializeComponent();

        Loaded += OnModalLoaded;
        IsVisibleChanged += OnModalVisibilityChanged;
    }

    private void OnModalLoaded(object sender, RoutedEventArgs e)
    {
        ResetScrollAndFocus();
    }

    private void OnModalVisibilityChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (Visibility == Visibility.Visible)
        {
            ResetScrollAndFocus();
        }
    }

    private void ResetScrollAndFocus()
    {
        Dispatcher.InvokeAsync(() =>
        {
            ModalScrollViewer?.ScrollToTop();
            ProductNameTextBox?.Focus();
        }, System.Windows.Threading.DispatcherPriority.Loaded);
    }
}

