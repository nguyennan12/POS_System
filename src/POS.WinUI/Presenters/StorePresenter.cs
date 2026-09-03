using POS.WinUI.ApiClients;
using POS.WinUI.ViewInterfaces;

namespace POS.WinUI.Presenters;

/// Điều phối giữa IStoreView và StoreApiClient.
/// Nhận View + ApiClient qua DI — có thể unit test bằng cách mock interface.
public sealed class StorePresenter
{
  private readonly IStoreView _view;
  private readonly StoreApiClient _storeApiClient;

  public StorePresenter(IStoreView view, StoreApiClient client)
  {
    _view = view;
    _storeApiClient = client;
  }

  public async Task LoadAllAsync()
  {
    // TODO: Gọi _storeClient.GetAllAsync()
  }
}
