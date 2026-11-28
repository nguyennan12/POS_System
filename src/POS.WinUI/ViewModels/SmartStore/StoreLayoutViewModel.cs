using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using POS.Contracts.V1.StoreLayouts;
using POS.WinUI.Core.ApiClients;
using POS.WinUI.Core.Services;
using POS.WinUI.ViewModels.Common;
using POS.WinUI.ViewModels.SmartStore.Models;
using Wpf.Ui.Controls;

namespace POS.WinUI.ViewModels.SmartStore;

/// <summary>
/// ViewModel chính điều phối Quản lý Mặt bằng, Kho kệ và Smart Picking
/// Tuân thủ quy chuẩn UI/UX pos_ui_guide.md và phân rã partial class.
/// </summary>
public sealed partial class StoreLayoutViewModel : ManagementTabViewModelBase
{
    private readonly StoreLayoutApiClient _layoutClient;
    private readonly ProductPlacementApiClient _placementClient;

    public override string TabId => "SmartStore";
    public override string Title => Loc["nav.smart_store"];
    public override string Subtitle => "Quản lý mặt bằng, kho kệ sơ đồ và Smart Picking";
    public override SymbolRegular Icon => SymbolRegular.Map24;

    // Sub-ViewModels
    public StoreMapViewModel MapVm { get; }
    public LayoutEditorViewModel EditorVm { get; }
    public ProductPlacementViewModel PlacementVm { get; }
    public PickingRouteViewModel PickingVm { get; }

    // Layout List & Selected
    public ObservableCollection<StoreLayoutSummaryItem> Layouts { get; } = [];

    [ObservableProperty]
    private StoreLayoutSummaryItem? _selectedLayoutSummary;

    [ObservableProperty]
    private StoreLayoutDetailModel? _currentLayout;

    // View Mode: "Map" (View), "Design" (Grid & Shelves edit), "Placement" (SKU assignment), "Picking" (Route preview)
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsMapMode))]
    [NotifyPropertyChangedFor(nameof(IsDesignMode))]
    [NotifyPropertyChangedFor(nameof(IsPlacementMode))]
    [NotifyPropertyChangedFor(nameof(IsPickingMode))]
    private string _activeMode = "Map";

    public bool IsMapMode => ActiveMode == "Map";
    public bool IsDesignMode => ActiveMode == "Design";
    public bool IsPlacementMode => ActiveMode == "Placement";
    public bool IsPickingMode => ActiveMode == "Picking";

    // Dialog state - Create
    [ObservableProperty] private bool _isCreateDialogOpen;
    [ObservableProperty] private string _newLayoutName = string.Empty;
    [ObservableProperty] private int _newLayoutWidth = 20;
    [ObservableProperty] private int _newLayoutHeight = 15;
    [ObservableProperty] private bool _newLayoutCopyFromActive;

    // Dialog state - Clone
    [ObservableProperty] private bool _isCloneDialogOpen;
    [ObservableProperty] private string _cloneLayoutName = string.Empty;
    [ObservableProperty] private Guid? _cloneTargetStoreId;
    [ObservableProperty] private bool _cloneCopyPlacements;

    // Dialog state - Confirmations
    [ObservableProperty] private bool _isPublishConfirmOpen;
    [ObservableProperty] private bool _isArchiveConfirmOpen;

    public bool CanWrite => HasPermission("store_layouts:write") || IsOwner || IsStoreManager;
    public bool CanPublish => HasPermission("store_layouts:publish") || IsOwner || IsStoreManager;
    public bool CanEditCurrentLayout => CanWrite && CurrentLayout?.IsDraft == true;
    public bool CanPublishCurrentLayout => CanPublish && CurrentLayout?.IsDraft == true;

    public StoreLayoutViewModel(
        StoreLayoutApiClient layoutClient,
        ProductPlacementApiClient placementClient,
        SessionService sessionService,
        StoreMapViewModel mapVm,
        LayoutEditorViewModel editorVm,
        ProductPlacementViewModel placementVm,
        PickingRouteViewModel pickingVm) : base(sessionService)
    {
        _layoutClient = layoutClient;
        _placementClient = placementClient;
        MapVm = mapVm;
        EditorVm = editorVm;
        PlacementVm = placementVm;
        PickingVm = pickingVm;

        WireSubViewModelEvents();

        _ = LoadLayoutsAsync();
    }

    [RelayCommand]
    public void SwitchMode(string mode)
    {
        ActiveMode = mode;
        if (mode == "Placement" && CurrentLayout != null)
        {
            PlacementVm.Initialize(CurrentLayout);
        }
        else if (mode == "Design" && CurrentLayout != null)
        {
            EditorVm.Initialize(CurrentLayout);
        }
        else if (mode == "Picking" && CurrentLayout != null)
        {
            PickingVm.Initialize(CurrentLayout);
        }
    }

    public override void Dispose()
    {
        UnwireSubViewModelEvents();
        base.Dispose();
    }
}
