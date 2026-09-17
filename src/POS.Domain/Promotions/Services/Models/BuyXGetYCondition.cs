namespace POS.Domain.Promotions.Services.Models;

public record BuyXGetYCondition(
    int BuyQuantity = 1,
    int GetQuantity = 1,
    Guid? FreeSkuId = null,
    decimal DiscountPercent = 100);
