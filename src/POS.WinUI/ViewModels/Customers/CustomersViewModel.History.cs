using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using POS.Contracts.V1.Customers;

namespace POS.WinUI.ViewModels.Customers;

public sealed partial class CustomersViewModel
{
    public ObservableCollection<PointTransactionResponse> PointTransactions { get; } = [];

    [ObservableProperty] private CustomerSummaryResponse? _historySelectedCustomer;
    [ObservableProperty] private int _historyPageNumber = 1;
    [ObservableProperty] private int _historyPageSize = 20;
    [ObservableProperty] private int _historyTotalPages = 1;
    [ObservableProperty] private bool _isHistoryEmpty;

    public async Task LoadPointTransactionsAsync()
    {
        PointTransactions.Clear();
        IsLoading = true;
        ListError = null;

        try
        {
            var targetCustomerId = HistorySelectedCustomer?.Id ?? Customers.FirstOrDefault()?.Id;
            if (!targetCustomerId.HasValue)
            {
                IsHistoryEmpty = true;
                return;
            }

            var res = await _api.GetLoyaltyTransactionsAsync(
                id: targetCustomerId.Value,
                pageNumber: HistoryPageNumber,
                pageSize: HistoryPageSize
            );

            if (res?.Success == true && res.Data != null)
            {
                foreach (var tx in res.Data.Items)
                {
                    PointTransactions.Add(tx);
                }

                HistoryTotalPages = Math.Max(1, (int)Math.Ceiling((double)res.Data.TotalCount / HistoryPageSize));
                IsHistoryEmpty = PointTransactions.Count == 0;
            }
            else
            {
                IsHistoryEmpty = true;
            }
        }
        catch (Exception ex)
        {
            ListError = ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task ViewCustomerHistoryAsync(CustomerSummaryResponse customer)
    {
        if (customer == null) return;
        HistorySelectedCustomer = customer;
        IsHistoryTab = true; // Switch to History Tab
        HistoryPageNumber = 1;
        await LoadPointTransactionsAsync();
    }
}
