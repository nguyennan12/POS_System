using System;
using POS.Contracts.V1.Inventory;

namespace POS.WinUI.ViewModels.Inventory;

public class SupplierOptionModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }

    public string DisplayText => string.IsNullOrWhiteSpace(Phone) ? Name : $"{Name} ({Phone})";

    public static SupplierOptionModel FromDto(SupplierResponse dto)
    {
        return new SupplierOptionModel
        {
            Id = dto.Id,
            Name = dto.Name,
            Phone = dto.Phone
        };
    }
}
