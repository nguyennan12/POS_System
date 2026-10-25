using System;
using CommunityToolkit.Mvvm.ComponentModel;
using POS.Contracts.V1.Inventory;

namespace POS.WinUI.ViewModels.Inventory;

public partial class StockTakeItemModel : ObservableObject
{
    public Guid Id { get; set; }
    public Guid StoreId { get; set; }

    [ObservableProperty]
    private string _status = "Draft";

    public string? Note { get; set; }
    public Guid CreatedBy { get; set; }
    public Guid? ApprovedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ApprovedAt { get; set; }

    public string CreatedAtFormatted => CreatedAt.ToLocalTime().ToString("dd/MM/yyyy HH:mm");
    public string? ApprovedAtFormatted => ApprovedAt?.ToLocalTime().ToString("dd/MM/yyyy HH:mm");

    public bool CanEdit => Status == "Draft";
    public bool CanSubmit => Status == "Draft";
    public bool CanApprove => Status == "Pending";

    public string StatusText => Status switch
    {
        "Draft" => "Bản nháp",
        "Pending" => "Chờ duyệt",
        "Approved" => "Đã duyệt cân bằng kho",
        _ => Status
    };

    public string StatusBadgeBg => Status switch
    {
        "Draft" => "#FEF3C7",
        "Pending" => "#E0E7FF",
        "Approved" => "#ECFDF5",
        _ => "#F1F5F9"
    };

    public string StatusBadgeFg => Status switch
    {
        "Draft" => "#D97706",
        "Pending" => "#4338CA",
        "Approved" => "#10B981",
        _ => "#64748B"
    };

    public static StockTakeItemModel FromDto(StockTakeSummaryResponse dto)
    {
        return new StockTakeItemModel
        {
            Id = dto.Id,
            StoreId = dto.StoreId,
            Status = dto.Status,
            Note = dto.Note,
            CreatedBy = dto.CreatedBy,
            ApprovedBy = dto.ApprovedBy,
            CreatedAt = dto.CreatedAt,
            ApprovedAt = dto.ApprovedAt
        };
    }
}
