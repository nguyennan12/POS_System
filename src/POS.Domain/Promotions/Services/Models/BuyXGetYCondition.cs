namespace POS.Domain.Promotions.Services.Models;

/// <summary>Defines quantities and discount behavior for a buy-X-get-Y promotion.</summary>
public record BuyXGetYCondition(
    int BuyQuantity = 1,
    int GetQuantity = 1,
    Guid? FreeSkuId = null,
    decimal DiscountPercent = 100);
