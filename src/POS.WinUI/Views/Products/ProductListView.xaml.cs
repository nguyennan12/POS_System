using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using POS.WinUI.Models;
using POS.WinUI.ViewModels.Products;

namespace POS.WinUI.Views.Products;

public partial class ProductListView : UserControl
{
    private DispatcherTimer? _searchDebounce;

    public ProductListView()
    {
        InitializeComponent();
        DataContext = App.Services.GetService(typeof(ProductListViewModel)) as ProductListViewModel
                     ?? throw new InvalidOperationException("ProductListViewModel not registered.");
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is ProductListViewModel vm)
            await vm.InitializeCommand.ExecuteAsync(null);
    }

    // ── Debounced search (300ms) ──────────────────────────────────────────────
    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        _searchDebounce?.Stop();
        _searchDebounce = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
        _searchDebounce.Tick += async (_, _) =>
        {
            _searchDebounce?.Stop();
            if (DataContext is ProductListViewModel vm)
                await vm.SearchCommand.ExecuteAsync(null);
        };
        _searchDebounce.Start();
    }

    // ── Status filter ─────────────────────────────────────────────────────────
    private void OnStatusSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox cb && cb.SelectedItem is ComboBoxItem item && DataContext is ProductListViewModel vm)
        {
            vm.SelectedStatus = (item.Tag as string) ?? string.Empty;
        }
    }

    // ── Double-click row → Edit ───────────────────────────────────────────────
    private async void OnRowDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is ProductListViewModel vm && vm.SelectedProduct != null)
            await vm.OpenEditCommand.ExecuteAsync(vm.SelectedProduct);
    }
}
