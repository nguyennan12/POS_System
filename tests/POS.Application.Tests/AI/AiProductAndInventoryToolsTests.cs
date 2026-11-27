using System.Text.Json;
using FluentAssertions;
using MediatR;
using NSubstitute;
using POS.Application.Abstractions.Auth;
using POS.Application.UseCases.AI.Tools.Common;
using POS.Application.UseCases.AI.Tools.Inventory;
using POS.Application.UseCases.AI.Tools.Products;
using POS.Application.UseCases.Inventory;
using POS.Application.UseCases.Inventory.Stock.Queries.GetInventorySummary;
using POS.Application.UseCases.Inventory.Stock.Queries.GetStockAlerts;
using POS.Application.UseCases.Products;
using POS.Application.UseCases.Products.Queries.GetPosCatalog;
using POS.Application.UseCases.Products.Queries.GetProductById;
using POS.Application.UseCases.Stores.Errors;
using POS.Domain.Common;

namespace POS.Application.Tests.AI;

public sealed class AiProductAndInventoryToolsTests
{
    [Fact]
    public void ProductQueries_EnforceProductsReadPermission()
    {
        IRequirePermission catalogQuery = new GetPosCatalogQuery(null, "cà phê", 1, 10);
        IRequirePermission detailQuery = new GetProductByIdQuery(Guid.NewGuid());

        catalogQuery.RequiredPermission.Should().Be("products:read");
        detailQuery.RequiredPermission.Should().Be("products:read");
    }

    [Fact]
    public async Task SearchProductsTool_ExcludesCostPrice_AndReturnsEmptyListWhenNoMatch()
    {
        var sender = Substitute.For<ISender>();
        var productId = Guid.NewGuid();
        var skuId = Guid.NewGuid();

        sender.Send(Arg.Any<GetPosCatalogQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result<PagedPosCatalogList>.Success(new PagedPosCatalogList(
                [
                    new PosCatalogDto(
                        SkuId: skuId,
                        ProductId: productId,
                        ProductName: "Cà phê sữa đá",
                        SkuCode: "CF-SUA",
                        Barcode: "8930001",
                        SellPrice: 29000m,
                        CostPrice: 11500m, // Phải bị loại bỏ khỏi JSON trả cho AI
                        TaxRate: 8m,
                        QtyOnHand: 42m,
                        BaseUnit: "Ly",
                        CategoryId: Guid.NewGuid(),
                        CategoryName: "Đồ uống",
                        ImageUrl: null)
                ],
                1,
                1,
                10)));

        var handler = new SearchProductsToolHandler(sender);
        using var args = JsonDocument.Parse("""{"keyword":"  Cà phê sữa  ","limit":5}""");

        var result = await handler.ExecuteAsync(args.RootElement, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.ResultCount.Should().Be(1);

        var json = JsonSerializer.Serialize(result, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        json.Should().Contain("29000");
        json.Should().NotContain("costPrice", "CostPrice must never be exposed");
        json.Should().NotContain("11500");

        await sender.Received(1).Send(
            Arg.Is<GetPosCatalogQuery>(q => q.Search == "Cà phê sữa" && q.PageSize == 5 && q.StoreId == null),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetProductDetailTool_ExcludesCostPrice_AndMapsNotFoundWhenOtherStoreOrMissing()
    {
        var sender = Substitute.For<ISender>();
        var productId = Guid.NewGuid();
        var missingId = Guid.NewGuid();

        sender.Send(Arg.Is<GetProductByIdQuery>(q => q.Id == productId), Arg.Any<CancellationToken>())
            .Returns(Result<ProductDetailDto>.Success(new ProductDetailDto(
                Id: productId,
                CategoryId: Guid.NewGuid(),
                CategoryName: "Nước giải khát",
                Name: "Coca Cola",
                Description: "Lon 330ml",
                Brand: "Coca-Cola",
                BaseUnit: "Lon",
                ImageUrl: null,
                Status: "Active",
                Skus:
                [
                    new SkuDetailDto(
                        Id: Guid.NewGuid(),
                        ProductId: productId,
                        ProductName: "Coca Cola",
                        SkuCode: "COCA-330",
                        Barcode: "8935049501503",
                        Attributes: null,
                        CostPrice: 6800m, // Phải bị loại bỏ
                        SellPrice: 10000m,
                        TaxRate: 8m,
                        IsActive: true,
                        QtyOnHand: 120m,
                        UnitConversions:
                        [
                            new UnitConversionDto(Guid.NewGuid(), Guid.NewGuid(), "Thùng", 24m, 225000m)
                        ],
                        CreatedAt: DateTime.UtcNow,
                        UpdatedAt: DateTime.UtcNow)
                ],
                CreatedAt: DateTime.UtcNow,
                UpdatedAt: DateTime.UtcNow)));

        sender.Send(Arg.Is<GetProductByIdQuery>(q => q.Id == missingId), Arg.Any<CancellationToken>())
            .Returns(Result<ProductDetailDto>.Failure(ProductErrors.ProductNotFound(missingId)));

        var handler = new GetProductDetailToolHandler(sender);

        using var okArgs = JsonDocument.Parse($$"""{"productId":"{{productId}}"}""");
        var okResult = await handler.ExecuteAsync(okArgs.RootElement, CancellationToken.None);
        okResult.Success.Should().BeTrue();
        var okJson = JsonSerializer.Serialize(okResult, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        okJson.Should().NotContain("costPrice");
        okJson.Should().NotContain("6800");
        okJson.Should().Contain("Th\\u00F9ng");

        using var missingArgs = JsonDocument.Parse($$"""{"productId":"{{missingId}}"}""");
        var missingResult = await handler.ExecuteAsync(missingArgs.RootElement, CancellationToken.None);
        missingResult.Success.Should().BeFalse();
        missingResult.ErrorCode.Should().Be(AiToolErrors.NotFound);
    }

    [Fact]
    public async Task GetInventoryTool_EnforcesStore_ExcludesAverageCost_AndReturnsNotFoundWhenSkuNotInStore()
    {
        var storeA = Guid.NewGuid();
        var skuInStoreA = Guid.NewGuid();
        var skuInStoreB = Guid.NewGuid();

        var sender = Substitute.For<ISender>();
        var storePolicy = Substitute.For<IStoreAccessPolicy>();
        storePolicy.EnforceStoreAccess(null).Returns(Result<Guid>.Success(storeA));

        sender.Send(
                Arg.Is<GetInventorySummaryQuery>(q => q.SkuId == skuInStoreA && q.StoreId == storeA),
                Arg.Any<CancellationToken>())
            .Returns(Result<PagedStockEntryList>.Success(new PagedStockEntryList(
                [
                    new StockEntryDto(
                        Id: Guid.NewGuid(),
                        SkuId: skuInStoreA,
                        SkuCode: "COCA-330",
                        Barcode: "8935049501503",
                        ProductName: "Coca 330ml",
                        QtyOnHand: 48m,
                        MinStock: 12m,
                        AverageCost: 7100m, // Phải bị loại bỏ
                        LastUpdated: DateTimeOffset.UtcNow)
                ],
                1,
                1,
                20)));

        sender.Send(
                Arg.Is<GetInventorySummaryQuery>(q => q.SkuId == skuInStoreB && q.StoreId == storeA),
                Arg.Any<CancellationToken>())
            .Returns(Result<PagedStockEntryList>.Success(new PagedStockEntryList([], 0, 1, 20)));

        var handler = new GetInventoryToolHandler(sender, storePolicy);

        using var storeAArgs = JsonDocument.Parse($$"""{"skuId":"{{skuInStoreA}}"}""");
        var okResult = await handler.ExecuteAsync(storeAArgs.RootElement, CancellationToken.None);
        okResult.Success.Should().BeTrue();
        var okJson = JsonSerializer.Serialize(okResult, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        okJson.Should().NotContain("averageCost");
        okJson.Should().NotContain("7100");
        okJson.Should().Contain("\"qtyOnHand\":48");

        // Store A không đọc được SKU của Store B
        using var storeBArgs = JsonDocument.Parse($$"""{"skuId":"{{skuInStoreB}}"}""");
        var notFoundResult = await handler.ExecuteAsync(storeBArgs.RootElement, CancellationToken.None);
        notFoundResult.Success.Should().BeFalse();
        notFoundResult.ErrorCode.Should().Be(AiToolErrors.NotFound);

        // Chain Owner chưa chọn X-Store-Id -> STORE_REQUIRED
        storePolicy.EnforceStoreAccess(null).Returns(Result<Guid>.Failure(StoreErrors.StoreRequired));
        var noStoreResult = await handler.ExecuteAsync(storeAArgs.RootElement, CancellationToken.None);
        noStoreResult.Success.Should().BeFalse();
        noStoreResult.ErrorCode.Should().Be(AiToolErrors.StoreRequired);
    }

    [Fact]
    public async Task GetStockAlertsTool_ScopesToCurrentStore_AndRespectsLimit()
    {
        var storeId = Guid.NewGuid();
        var sender = Substitute.For<ISender>();
        var storePolicy = Substitute.For<IStoreAccessPolicy>();
        storePolicy.EnforceStoreAccess(null).Returns(Result<Guid>.Success(storeId));

        sender.Send(
                Arg.Is<GetStockAlertsQuery>(q => q.NearExpiryDays == 14 && q.PageSize == 2 && q.StoreId == storeId),
                Arg.Any<CancellationToken>())
            .Returns(Result<PagedStockAlertList>.Success(new PagedStockAlertList(
                [
                    new StockAlertDto(Guid.NewGuid(), "SKU-1", "111", "Sữa tươi", 3m, 10m, "MinStock"),
                    new StockAlertDto(Guid.NewGuid(), "SKU-1", "111", "Sữa tươi", 3m, 10m, "NearExpiry"),
                    new StockAlertDto(Guid.NewGuid(), "SKU-2", "222", "Bánh mì", 1m, 5m, "MinStock")
                ],
                2,
                1,
                2)));

        var handler = new GetStockAlertsToolHandler(sender, storePolicy);
        using var args = JsonDocument.Parse("""{"nearExpiryDays":14,"limit":2}""");

        var result = await handler.ExecuteAsync(args.RootElement, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.ResultCount.Should().Be(2);
    }
}
