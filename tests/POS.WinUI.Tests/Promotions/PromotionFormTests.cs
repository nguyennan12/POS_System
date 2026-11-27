using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using POS.Contracts.V1.Categories;
using POS.Contracts.V1.Common;
using POS.Contracts.V1.Products;
using POS.Contracts.V1.Promotions;
using POS.WinUI.Core.ApiClients;
using POS.WinUI.Core.Services;
using POS.WinUI.ViewModels.Promotions;

namespace POS.WinUI.Tests.Promotions;

public sealed class PromotionFormTests
{
    [Theory]
    [InlineData("CartPercent")]
    [InlineData("CartFixed")]
    [InlineData("PercentSku")]
    [InlineData("FixedSku")]
    [InlineData("BuyXGetY")]
    [InlineData("HappyHour")]
    public void Create_form_sends_backend_type_codes(string type)
    {
        var form = new PromotionFormDialogViewModel { Name = "Khuyến mãi", ValueText = "10" };
        Assert.Equal(6, form.TypeOptions.Count);
        form.SelectedType = form.TypeOptions.Single(option => option.Code == type);

        Assert.True(form.TryCreateRequest(out var request), form.ErrorMessage);
        Assert.Equal(type, request.Type);
        Assert.Equal("All", request.AppliesTo);
        if (type == "BuyXGetY")
        {
            using var conditions = JsonDocument.Parse(request.ConditionsJson!);
            Assert.Equal(JsonValueKind.Null, conditions.RootElement.GetProperty("freeSkuId").ValueKind);
        }
    }

    [Theory]
    [InlineData("BuyXGetY", "SKU")]
    [InlineData("HappyHour", "Category")]
    public async Task Create_and_reopen_via_promotion_commands_preserves_store_targets_and_conditions(string type, string scope)
    {
        using var handler = new PromotionApiHandler();
        var session = new SessionService();
        session.SetSession("token", "refresh", Guid.NewGuid().ToString(), "Manager", "StoreManager", handler.StoreId.ToString());
        using var promotions = handler.CreateClient();
        using var categories = handler.CreateClient();
        using var products = handler.CreateClient();
        using var viewModel = new PromotionsViewModel(new PromotionApiClient(promotions, session),
            new CategoryApiClient(categories, session), new ProductApiClient(products, session), session, new ConfirmDialogStub());

        await viewModel.OpenCreatePromotionCommand.ExecuteAsync(null);
        var form = Assert.IsType<PromotionFormDialogViewModel>(viewModel.PromotionFormDialog);
        Assert.True(string.IsNullOrEmpty(form.CatalogMessage), form.CatalogMessage);
        Assert.Equal(handler.StoreId, form.CatalogStoreId);
        form.Name = "Khuyến mãi mới";
        form.SelectedType = form.TypeOptions.Single(option => option.Code == type);
        form.SelectedScope = form.ScopeOptions.Single(option => option.Code == scope);
        form.PriorityText = "15";
        form.IsStackable = true;
        form.IsExclusive = true;
        form.ValidTo = form.ValidFrom!.Value.AddDays(13);
        if (type == "BuyXGetY")
        {
            form.SkuTargets.Single(option => option.Id == handler.BuySkuId).IsSelected = true;
            Assert.Contains(form.FreeSkuOptions, option => option.Id == handler.GiftSkuId);
            form.BuyQuantityText = "2";
            form.GetQuantityText = "1";
            form.FreeSkuId = handler.GiftSkuId;
            form.DiscountPercentText = "75";
        }
        else
        {
            form.CategoryTargets.Single(option => option.Id == handler.CategoryId).IsSelected = true;
            form.ValueText = "20";
            form.StartTimeText = "09:30";
            form.EndTimeText = "11:45";
            form.WeekdayOptions.Single(day => day.Day == DayOfWeek.Monday).IsSelected = true;
            form.WeekdayOptions.Single(day => day.Day == DayOfWeek.Sunday).IsSelected = true;
        }

        await viewModel.SavePromotionCommand.ExecuteAsync(null);

        var created = Assert.IsType<CreatePromotionRequest>(handler.Created);
        Assert.Null(viewModel.PromotionFormDialog);
        Assert.Equal(type, created.Type);
        Assert.Equal(scope, created.AppliesTo);
        Assert.Equal(15, created.Priority);
        Assert.True(created.IsStackable);
        Assert.True(created.IsExclusive);
        using var json = JsonDocument.Parse(created.ConditionsJson!);
        if (type == "BuyXGetY")
        {
            Assert.Equal(handler.BuySkuId, Assert.Single(created.TargetSkuIds!));
            Assert.Empty(created.TargetCategoryIds!);
            Assert.Equal(2, json.RootElement.GetProperty("buyQuantity").GetInt32());
            Assert.Equal(1, json.RootElement.GetProperty("getQuantity").GetInt32());
            Assert.Equal(handler.GiftSkuId, json.RootElement.GetProperty("freeSkuId").GetGuid());
            Assert.Equal(75m, json.RootElement.GetProperty("discountPercent").GetDecimal());
        }
        else
        {
            Assert.Equal(handler.CategoryId, Assert.Single(created.TargetCategoryIds!));
            Assert.Empty(created.TargetSkuIds!);
            Assert.Equal("09:30:00", json.RootElement.GetProperty("startTime").GetString());
            Assert.Equal("11:45:00", json.RootElement.GetProperty("endTime").GetString());
            Assert.Equal([1, 0], json.RootElement.GetProperty("daysOfWeek").EnumerateArray().Select(day => day.GetInt32()));
        }

        await viewModel.OpenEditPromotionCommand.ExecuteAsync(Assert.Single(viewModel.Promotions));
        var editForm = Assert.IsType<PromotionFormDialogViewModel>(viewModel.PromotionFormDialog);
        Assert.True(editForm.IsEdit);
        Assert.Equal(scope, editForm.SelectedScope.Code);
        Assert.Equal("15", editForm.PriorityText);
        Assert.True(editForm.IsStackable);
        Assert.True(editForm.IsExclusive);
        if (type == "BuyXGetY")
        {
            Assert.Equal(handler.BuySkuId, Assert.Single(editForm.SkuTargets, option => option.IsSelected).Id);
            Assert.Equal(handler.GiftSkuId, editForm.FreeSkuId);
            Assert.Equal("2", editForm.BuyQuantityText);
            Assert.Equal("1", editForm.GetQuantityText);
            Assert.Equal("75", editForm.DiscountPercentText);
        }
        else
        {
            Assert.Equal(handler.CategoryId, Assert.Single(editForm.CategoryTargets, option => option.IsSelected).Id);
            Assert.Equal("09:30:00", editForm.StartTimeText);
            Assert.Equal("11:45:00", editForm.EndTimeText);
            Assert.Equal([DayOfWeek.Monday, DayOfWeek.Sunday],
                editForm.WeekdayOptions.Where(day => day.IsSelected).Select(day => day.Day));
        }
        editForm.Name = "Khuyến mãi chỉnh sửa";

        await viewModel.SavePromotionCommand.ExecuteAsync(null);

        var updated = Assert.IsType<UpdatePromotionRequest>(handler.Updated);
        Assert.Null(viewModel.PromotionFormDialog);
        Assert.Equal("Khuyến mãi chỉnh sửa", updated.Name);
        Assert.Equal(created.TargetCategoryIds!.ToArray(), updated.TargetCategoryIds!.ToArray());
        Assert.Equal(created.TargetSkuIds!.ToArray(), updated.TargetSkuIds!.ToArray());
        Assert.Equal(created.ConditionsJson, updated.ConditionsJson);
        Assert.Equal(created.ValidFrom, updated.ValidFrom);
        Assert.Equal(created.ValidTo, updated.ValidTo);
        Assert.Equal(2, handler.SecondPageRequests);
    }

    [Fact]
    public void Editing_an_existing_condition_keeps_extra_data_and_unavailable_targets()
    {
        var now = DateTimeOffset.UtcNow;
        var skuId = Guid.NewGuid();
        var giftId = Guid.NewGuid();
        var original = new PromotionDetailResponse(Guid.NewGuid(), Guid.NewGuid(), "Ưu đãi cũ", "BuyXGetY",
            1m, 0m, null, $$"""{"BuyQuantity":2,"GetQuantity":1,"FreeSkuId":"{{giftId}}","CustomRule":true}""",
            0, false, false, "SKU", now, null, "Active", [], [skuId], now);
        var form = new PromotionFormDialogViewModel(original);
        Assert.True(form.TryUpdateRequest(out var unchanged), form.ErrorMessage);
        Assert.Equal(original.ConditionsJson, unchanged.ConditionsJson);
        form.GetQuantityText = "3";

        Assert.True(form.TryUpdateRequest(out var changed), form.ErrorMessage);

        Assert.Equal(skuId, Assert.Single(changed.TargetSkuIds!));
        using var json = JsonDocument.Parse(changed.ConditionsJson!);
        Assert.True(json.RootElement.GetProperty("CustomRule").GetBoolean());
        Assert.Equal(3, json.RootElement.GetProperty("getQuantity").GetInt32());
        Assert.Equal(giftId, json.RootElement.GetProperty("freeSkuId").GetGuid());
    }

    private sealed class PromotionApiHandler : HttpMessageHandler
    {
        private readonly Guid _promotionId = Guid.NewGuid();
        public Guid StoreId { get; } = Guid.NewGuid();
        public Guid CategoryId { get; } = Guid.NewGuid();
        public Guid BuySkuId { get; } = Guid.NewGuid();
        public Guid GiftSkuId { get; } = Guid.NewGuid();
        public CreatePromotionRequest? Created { get; private set; }
        public UpdatePromotionRequest? Updated { get; private set; }
        public int SecondPageRequests { get; private set; }

        public HttpClient CreateClient() => new(this, disposeHandler: false) { BaseAddress = new Uri("http://localhost/") };

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var uri = request.RequestUri!;
            Assert.Equal(StoreId.ToString(), Assert.Single(request.Headers.GetValues("X-Store-Id")));
            switch (uri.AbsolutePath)
            {
                case "/api/v1/categories":
                    Assert.Contains($"storeId={StoreId}", uri.Query);
                    var child = new CategoryResponse(CategoryId, null, "Danh mục con", 0, null, true, DateTimeOffset.UtcNow);
                    return Json(new CategoryResponse[] { new(Guid.NewGuid(), null, "Danh mục gốc", 0, null, true, child.CreatedAt, [child]) });

                case "/api/v1/products/pos-catalog":
                    Assert.Contains($"storeId={StoreId}", uri.Query);
                    var secondPage = uri.Query.Contains("pageNumber=2", StringComparison.Ordinal);
                    if (secondPage) SecondPageRequests++;
                    return Json(new PagedResponse<PosCatalogItemResponse>(
                        [secondPage ? CatalogSku(GiftSkuId, "GIFT") : CatalogSku(BuySkuId, "BUY")], secondPage ? 2 : 1, 200, 201));

                case "/api/v1/promotions" when request.Method == HttpMethod.Post:
                    Created = await request.Content!.ReadFromJsonAsync<CreatePromotionRequest>(cancellationToken);
                    return Json(Detail());

                case "/api/v1/promotions":
                    var detail = Created is null ? null : Detail();
                    PromotionSummaryResponse[] rows = detail is null ? [] :
                        [new(detail.Id, detail.StoreId, detail.Name, detail.Type, detail.Value, detail.AppliesTo,
                            detail.ValidFrom, detail.ValidTo, detail.Status, detail.CreatedAt)];
                    return Json(new PagedResponse<PromotionSummaryResponse>(rows, 1, 15, rows.Length));
            }

            Assert.Equal($"/api/v1/promotions/{_promotionId}", uri.AbsolutePath);
            if (request.Method == HttpMethod.Put)
                Updated = await request.Content!.ReadFromJsonAsync<UpdatePromotionRequest>(cancellationToken);
            return Json(Detail());
        }

        private PosCatalogItemResponse CatalogSku(Guid id, string code) =>
            new(id, Guid.NewGuid(), code, code, code, 100m, 0m, 0m, 0m, "cái", CategoryId, "Danh mục con", null);

        private PromotionDetailResponse Detail()
        {
            var create = Created!;
            return new PromotionDetailResponse(_promotionId, StoreId, Updated?.Name ?? create.Name,
                create.Type, create.Value, create.MinOrderAmount, create.MaxDiscountAmount,
                create.ConditionsJson, create.Priority, create.IsStackable, create.IsExclusive, create.AppliesTo,
                create.ValidFrom!.Value, create.ValidTo, "Active", create.TargetCategoryIds!, create.TargetSkuIds!, DateTimeOffset.UtcNow);
        }

        private static HttpResponseMessage Json<T>(T data) =>
            new(HttpStatusCode.OK) { Content = JsonContent.Create(ApiResponse<T>.Ok(data)) };
    }

    private sealed class ConfirmDialogStub : IConfirmDialogService
    {
        public Task<bool> ShowConfirmAsync(ConfirmDialogOptions options) => Task.FromResult(false);

        public Task<bool> ShowDangerConfirmAsync(string title, string message, string? warningMessage, string confirmText) =>
            Task.FromResult(false);
    }
}
