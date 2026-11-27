using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Persistence;
using POS.Application.UseCases.Orders.Errors;
using POS.Application.UseCases.Promotions.Queries;
using POS.Application.UseCases.Promotions.Queries.GetActivePromotionDetails;
using POS.Application.UseCases.Promotions.Queries.GetApplicablePromotionsForSku;
using POS.Domain.Common;
using POS.Domain.Products;
using POS.Domain.Promotions;
using POS.Domain.Promotions.Enums;

namespace POS.Application.Tests.Promotions;

public sealed class PromotionReadQueryTests
{
    private readonly Guid storeId = Guid.NewGuid();
    private readonly IStoreAccessPolicy storeAccess = Substitute.For<IStoreAccessPolicy>();
    private readonly IPromotionRepository promotions = Substitute.For<IPromotionRepository>();
    private readonly ICategoryRepository categories = Substitute.For<ICategoryRepository>();
    private readonly ISkuRepository skus = Substitute.For<ISkuRepository>();
    private readonly PromotionDetailsReader reader;

    public PromotionReadQueryTests()
    {
        storeAccess.EnforceStoreAccess().Returns(Result<Guid>.Success(storeId));
        reader = new PromotionDetailsReader(categories, skus, NullLogger<PromotionDetailsReader>.Instance);
    }

    [Fact]
    public async Task Active_details_handle_malformed_conditions_and_keep_targets_in_the_current_store()
    {
        var category = Category.Create(storeId, "Thực phẩm");
        var foreignCategory = Category.Create(Guid.NewGuid(), "Chi nhánh khác");
        var broken = new Promotion(storeId, "Giờ vàng cũ", PromotionType.HappyHour, 10m,
            conditionsJson: "{broken", appliesTo: PromotionAppliesTo.Category);
        broken.AddTargetCategory(category.Id);
        broken.AddTargetCategory(foreignCategory.Id);
        var happyHour = new Promotion(storeId, "Giờ vàng", PromotionType.HappyHour, 15m,
            conditionsJson: """{"startTime":"09:00:00","endTime":"10:30:00","daysOfWeek":[1]}""");
        var otherType = new Promotion(storeId, "Giảm đơn", PromotionType.CartFixed, 10m);
        categories.GetAllByStoreIdAsync(storeId, Arg.Any<CancellationToken>()).Returns([category, foreignCategory]);
        SetActivePromotions(otherType, broken, happyHour);
        var handler = new GetActivePromotionDetailsQueryHandler(storeAccess, promotions, reader);
        var query = new GetActivePromotionDetailsQuery(PromotionType.HappyHour, Limit: 2);

        var result = await handler.Handle(query, CancellationToken.None);

        query.RequiredPermission.Should().Be("promotions:read");
        result.IsSuccess.Should().BeTrue();
        result.Value!.Select(item => item.PromotionId).Should().Equal(broken.Id, happyHour.Id);
        var brokenDetail = result.Value![0];
        brokenDetail.HappyHour.Should().BeNull();
        brokenDetail.TargetCategories.Should().ContainSingle().Which.Name.Should().Be(category.Name);
        var parsed = result.Value[1];
        parsed.HappyHour!.DaysOfWeek.Should().Equal(DayOfWeek.Monday);
        parsed.DescriptionVi.Should().Contain("09:00").And.Contain("10:30");
    }

    [Fact]
    public async Task Sku_query_returns_potential_matches_and_resolves_the_gift()
    {
        var category = Category.Create(storeId, "Bánh kẹo");
        var product = new Product(storeId, category.Id, "Bánh", "hộp");
        var sku = new Sku(product.Id, storeId, "BX", "01", 100m) { Product = product };
        var gift = new Sku(product.Id, storeId, "GIFT", "02", 40m) { Product = product };
        var all = new Promotion(Guid.Empty, "Giảm toàn đơn", PromotionType.CartPercent, 10m);
        var categoryMatch = new Promotion(storeId, "Giảm danh mục", PromotionType.FixedSku, 10m,
            appliesTo: PromotionAppliesTo.Category);
        categoryMatch.AddTargetCategory(category.Id);
        var skuMatch = new Promotion(storeId, "Mua tặng", PromotionType.BuyXGetY, 1m,
            conditionsJson: $$"""{"buyQuantity":2,"getQuantity":1,"freeSkuId":"{{gift.Id}}"}""",
            appliesTo: PromotionAppliesTo.SKU);
        skuMatch.AddTargetSku(sku.Id);
        var otherSku = new Promotion(storeId, "Giảm SKU khác", PromotionType.PercentSku, 10m,
            appliesTo: PromotionAppliesTo.SKU);
        otherSku.AddTargetSku(gift.Id);
        SetActivePromotions(all, categoryMatch, skuMatch, otherSku);
        skus.GetByIdWithProductAsync(sku.Id, Arg.Any<CancellationToken>()).Returns(sku);
        skus.GetByIdsWithProductAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>()).Returns([sku, gift]);
        categories.GetAllByStoreIdAsync(storeId, Arg.Any<CancellationToken>()).Returns([category]);
        var handler = new GetApplicablePromotionsForSkuQueryHandler(storeAccess, skus, promotions, reader);
        var query = new GetApplicablePromotionsForSkuQuery(sku.Id, 1.5m);

        var result = await handler.Handle(query, CancellationToken.None);

        query.RequiredPermission.Should().Be("promotions:read");
        result.IsSuccess.Should().BeTrue();
        result.Value!.Eligibility.Should().Be("potential");
        result.Value.Quantity.Should().Be(1.5m);
        result.Value.Promotions.Select(item => item.PromotionId).Should().Equal(all.Id, categoryMatch.Id, skuMatch.Id);
        var offer = result.Value.Promotions[2];
        offer.BuyXGetY!.FreeSkuId.Should().Be(gift.Id);
        offer.FreeSkuName.Should().Be("Bánh (GIFT)");
        offer.TargetSkus.Should().ContainSingle().Which.Id.Should().Be(sku.Id);
    }

    [Fact]
    public async Task Sku_query_returns_not_found_for_a_sku_from_another_store()
    {
        var sku = new Sku(Guid.NewGuid(), Guid.NewGuid(), "FOREIGN", "01", 100m);
        skus.GetByIdWithProductAsync(sku.Id, Arg.Any<CancellationToken>()).Returns(sku);
        var handler = new GetApplicablePromotionsForSkuQueryHandler(storeAccess, skus, promotions, reader);

        var result = await handler.Handle(new GetApplicablePromotionsForSkuQuery(sku.Id), CancellationToken.None);

        result.Error.Should().Be(OrderErrors.SkuNotFound);
        promotions.ReceivedCalls().Should().BeEmpty();
    }

    private void SetActivePromotions(params Promotion[] active) =>
        promotions.GetActivePromotionsAsync(storeId, Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(active);
}
