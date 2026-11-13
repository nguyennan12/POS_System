using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using POS.WinUI.ViewModels.Employees;

namespace POS.WinUI.Views.Employees;

public partial class EmployeesView : UserControl
{
    public EmployeesView()
    {
        InitializeComponent();
        CredentialOverlay.IsVisibleChanged += (_, _) => { CredentialInput.Clear(); if (CredentialOverlay.IsVisible) CredentialInput.Focus(); };
        CreateOverlay.IsVisibleChanged += (_, _) => { if (CreateOverlay.IsVisible) CreateOverlay.MoveFocus(new TraversalRequest(FocusNavigationDirection.First)); };
        DataContextChanged += async (_, e) =>
        {
            if (e.NewValue is EmployeesViewModel vm)
            {
                await vm.InitializeAsync();
                UpdateLayoutForSize();
            }
        };
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is EmployeesViewModel vm) await vm.InitializeAsync();
        UpdateLayoutForSize();
    }
    private void OnSizeChanged(object sender, SizeChangedEventArgs e) => UpdateLayoutForSize();
    private void UpdateLayoutForSize()
    {
        if (HeaderActions is null) return;
        var narrowHeader = ActualWidth < 1000;
        Grid.SetRow(HeaderActions, narrowHeader ? 1 : 0);
        Grid.SetColumn(HeaderActions, narrowHeader ? 0 : 1);
        Grid.SetColumnSpan(HeaderActions, narrowHeader ? 2 : 1);
        HeaderActions.HorizontalAlignment = narrowHeader ? HorizontalAlignment.Left : HorizontalAlignment.Right;
        HeaderActions.Margin = narrowHeader ? new Thickness(0, 16, 0, 0) : new Thickness(0);
        var stacked = ActualWidth < 860;
        ListColumn.Width = stacked ? new GridLength(1, GridUnitType.Star) : new GridLength(360);
        DetailColumn.Width = stacked ? new GridLength(0) : new GridLength(1, GridUnitType.Star);
        Grid.SetRow(DetailCard, stacked ? 1 : 0);
        Grid.SetColumn(DetailCard, stacked ? 0 : 1);
        Body.Height = stacked ? double.NaN : Math.Max(580, ActualHeight - Header.ActualHeight - 64);
        ListCard.Height = stacked ? 440 : double.NaN;
        DetailCard.Height = stacked ? 680 : double.NaN;
        ListCard.Margin = stacked ? new Thickness(0, 0, 0, 16) : new Thickness(0, 0, 16, 0);
    }
    private void OnCredentialChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is EmployeesViewModel vm) vm.NewCredential = ((PasswordBox)sender).Password;
    }
    private void OnBackdropCloseNewEmployee(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is EmployeesViewModel vm) vm.CloseNewEmployeeCommand.Execute(null);
    }

    private void OnBackdropCloseCredential(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is EmployeesViewModel vm) vm.CloseCredentialCommand.Execute(null);
    }

    private void OnCardMouseDown(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
    }

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Escape || DataContext is not EmployeesViewModel vm) return;
        if (vm.CredentialTitle != null) { vm.CloseCredentialCommand.Execute(null); e.Handled = true; }
        else if (vm.NewEmployee != null) { vm.CloseNewEmployeeCommand.Execute(null); e.Handled = true; }
    }
}
