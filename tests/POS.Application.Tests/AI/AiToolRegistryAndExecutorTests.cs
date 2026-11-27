using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using POS.Application.Abstractions.AI;
using POS.Application.UseCases.AI.Tools.Common;
using POS.Application.UseCases.Stores.Errors;
using POS.Domain.Common;
using POS.Domain.Orders.Enums;

namespace POS.Application.Tests.AI;

public sealed class AiToolRegistryAndExecutorTests
{
    private static readonly string[] ExpectedR1ToolNames =
    [
        "search_products",
        "get_product_detail",
        "get_inventory",
        "get_stock_alerts",
        "list_orders",
        "get_order_detail",
        "search_invoices",
        "get_invoice_detail",
        "get_revenue_report",
        "get_top_selling_products",
        "search_customers",
        "get_customer_detail",
        "get_customer_loyalty",
        "get_current_shift",
        "validate_voucher",
        "get_dashboard_summary",
        "get_slow_moving_products",
        "get_stock_history",
        "get_expiring_batches",
        "list_active_promotions",
        "get_product_promotions",
        "preview_order_promotions"
    ];

    [Fact]
    public void AiToolCall_ClonesJsonElement_SafelyAfterDocumentDispose()
    {
        AiToolCall call;
        using (var doc = JsonDocument.Parse("""{"keyword":"cà phê","limit":5}"""))
        {
            call = new AiToolCall("call_1", "search_products", doc.RootElement);
        }

        call.Arguments.GetProperty("keyword").GetString().Should().Be("cà phê");
        call.Arguments.GetProperty("limit").GetInt32().Should().Be(5);
    }

    [Fact]
    public void Registry_ContainsAll22UniqueStrictSchemas_InDeterministicOrder()
    {
        var registry = new AiToolRegistry();

        var tools = registry.GetAvailableTools();

        tools.Should().HaveCount(22);
        tools.Select(t => t.Name).Should().Equal(ExpectedR1ToolNames);

        foreach (var tool in tools)
        {
            tool.Type.Should().Be("function");
            tool.Description.Should().NotBeNullOrWhiteSpace();
            tool.Parameters.GetProperty("type").GetString().Should().Be("object");
            tool.Parameters.GetProperty("additionalProperties").GetBoolean().Should().BeFalse();

            if (tool.Parameters.TryGetProperty("properties", out var props))
            {
                var propNames = props.EnumerateObject().Select(p => p.Name).ToArray();
                propNames.Should().NotContain(["storeId", "employeeId", "role", "permission", "apiKey"]);
            }
        }
    }

    [Fact]
    public void Registry_SupportsAppendingR2AndR3Tools_AndFailsFastOnDuplicate()
    {
        var extraTool = AiToolSchemaFactory.CreateFunctionDefinition(
            "get_product_locations",
            "Tra cứu vị trí kệ hàng của SKU.",
            new Dictionary<string, object>
            {
                ["type"] = "object",
                ["properties"] = new Dictionary<string, object>
                {
                    ["skuId"] = new Dictionary<string, object> { ["type"] = "string", ["format"] = "uuid" }
                },
                ["required"] = new[] { "skuId" },
                ["additionalProperties"] = false
            });

        var extendedRegistry = new AiToolRegistry([extraTool]);
        extendedRegistry.GetAvailableTools().Should().HaveCount(23);
        extendedRegistry.GetAvailableTools().Last().Name.Should().Be("get_product_locations");

        var duplicateTool = AiToolSchemaFactory.CreateFunctionDefinition(
            "SEARCH_PRODUCTS",
            "Trùng tên không phân biệt hoa thường.",
            new Dictionary<string, object>
            {
                ["type"] = "object",
                ["properties"] = new Dictionary<string, object>(),
                ["additionalProperties"] = false
            });

        var act = () => new AiToolRegistry([duplicateTool]);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*SEARCH_PRODUCTS*");
    }

    [Fact]
    public void Executor_FailsFast_WhenDuplicateHandlersRegistered()
    {
        var h1 = new StubToolHandler("search_products", (_, _) => Task.FromResult(AiToolExecutionResult.EmptyList()));
        var h2 = new StubToolHandler("SEARCH_PRODUCTS", (_, _) => Task.FromResult(AiToolExecutionResult.EmptyList()));

        var act = () => new AiToolExecutor([h1, h2], NullLogger<AiToolExecutor>.Instance);
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public async Task Executor_HandlesUnknownInvalidExceptionAndCancellation_Deterministically()
    {
        var okHandler = new StubToolHandler(
            "search_products",
            (_, _) => Task.FromResult(AiToolExecutionResult.Ok(new[] { new { name = "Trà xanh" } }, 1)));
        var boomHandler = new StubToolHandler(
            "boom_tool",
            (_, _) => throw new InvalidOperationException("Sensitive SQL connection string leak"));
        var cancelHandler = new StubToolHandler(
            "cancel_tool",
            async (_, ct) =>
            {
                await Task.Delay(50, ct);
                return AiToolExecutionResult.EmptyList();
            });

        var executor = new AiToolExecutor([okHandler, boomHandler, cancelHandler], NullLogger<AiToolExecutor>.Instance);
        using var emptyObjDoc = JsonDocument.Parse("{}");
        using var arrayDoc = JsonDocument.Parse("[1,2,3]");

        // 1. Case-insensitive match
        var okResult = await executor.ExecuteAsync(
            new AiToolCall("call_ok", "SEARCH_PRODUCTS", emptyObjDoc.RootElement),
            CancellationToken.None);
        okResult.IsError.Should().BeFalse();
        okResult.Name.Should().Be("search_products");
        okResult.Result.Success.Should().BeTrue();
        okResult.Result.ResultCount.Should().Be(1);

        // 2. Unknown tool -> TOOL_NOT_FOUND
        var unknownResult = await executor.ExecuteAsync(
            new AiToolCall("call_missing", "non_existent_tool", emptyObjDoc.RootElement),
            CancellationToken.None);
        unknownResult.IsError.Should().BeTrue();
        unknownResult.Result.ErrorCode.Should().Be(AiToolErrors.ToolNotFound);

        // 3. Non-object JSON -> INVALID_ARGUMENTS
        var invalidJsonResult = await executor.ExecuteAsync(
            new AiToolCall("call_arr", "search_products", arrayDoc.RootElement),
            CancellationToken.None);
        invalidJsonResult.IsError.Should().BeTrue();
        invalidJsonResult.Result.ErrorCode.Should().Be(AiToolErrors.InvalidArguments);

        // 4. Handler exception -> QUERY_FAILED without leaking internal message
        var boomResult = await executor.ExecuteAsync(
            new AiToolCall("call_boom", "boom_tool", emptyObjDoc.RootElement),
            CancellationToken.None);
        boomResult.IsError.Should().BeTrue();
        boomResult.Result.ErrorCode.Should().Be(AiToolErrors.QueryFailed);
        boomResult.Result.ErrorMessage.Should().NotContain("Sensitive SQL");

        // 5. Request cancellation propagates OperationCanceledException
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var cancelAct = () => executor.ExecuteAsync(
            new AiToolCall("call_cancel", "cancel_tool", emptyObjDoc.RootElement),
            cts.Token);
        await cancelAct.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public void ArgumentReader_And_DomainErrorMapping_EnforceConstraintsAccurately()
    {
        var validId = Guid.NewGuid();
        using var doc = JsonDocument.Parse($$"""
        {
          "keyword": "  Coca 330ml  ",
          "skuId": "{{validId}}",
          "emptyGuid": "00000000-0000-0000-0000-000000000000",
          "limit": 25,
          "status": "paid",
          "numericEnum": "99",
          "from": "2026-11-20",
          "to": "2026-11-10"
        }
        """);
        var root = doc.RootElement;

        AiToolArgumentReader.TryReadRequiredString(root, "keyword", 1, 100, out var keyword, out _).Should().BeTrue();
        keyword.Should().Be("Coca 330ml");

        AiToolArgumentReader.TryReadRequiredGuid(root, "skuId", out var parsedGuid, out _).Should().BeTrue();
        parsedGuid.Should().Be(validId);

        AiToolArgumentReader.TryReadRequiredGuid(root, "emptyGuid", out _, out var emptyGuidErr).Should().BeFalse();
        emptyGuidErr!.ErrorCode.Should().Be(AiToolErrors.InvalidArguments);

        AiToolArgumentReader.TryReadOptionalInt(root, "limit", 10, 1, 20, out _, out var limitErr).Should().BeFalse();
        limitErr!.ErrorCode.Should().Be(AiToolErrors.InvalidArguments);

        AiToolArgumentReader.TryReadOptionalEnum<OrderStatus>(root, "status", out var status, out var canonical, out _).Should().BeTrue();
        status.Should().Be(OrderStatus.Paid);
        canonical.Should().Be("Paid");

        AiToolArgumentReader.TryReadOptionalEnum<OrderStatus>(root, "numericEnum", out _, out _, out var enumErr).Should().BeFalse();
        enumErr!.ErrorCode.Should().Be(AiToolErrors.InvalidArguments);

        AiToolArgumentReader.TryReadRequiredDateRangeUtc(root, "from", "to", out _, out _, out var rangeErr).Should().BeFalse();
        rangeErr!.ErrorCode.Should().Be(AiToolErrors.InvalidArguments);

        var allowed = new HashSet<string>(StringComparer.Ordinal) { "keyword" };
        AiToolArgumentReader.EnsureAllowedProperties(root, allowed, out var unknownPropErr).Should().BeFalse();
        unknownPropErr!.ErrorCode.Should().Be(AiToolErrors.InvalidArguments);

        // Domain error mappings
        AiToolErrors.FromDomainError(StoreErrors.StoreRequired).ErrorCode.Should().Be(AiToolErrors.StoreRequired);
        AiToolErrors.FromDomainError(StoreErrors.StoreAccessDenied).ErrorCode.Should().Be(AiToolErrors.PermissionDenied);
        AiToolErrors.FromDomainError(new Error(ErrorType.NotFound, "SKU.NotFound", "Không thấy")).ErrorCode.Should().Be(AiToolErrors.NotFound);
    }

    private sealed class StubToolHandler(
        string name,
        Func<JsonElement, CancellationToken, Task<AiToolExecutionResult>> handler) : IAiToolHandler
    {
        public string Name { get; } = name;

        public Task<AiToolExecutionResult> ExecuteAsync(JsonElement arguments, CancellationToken cancellationToken) =>
            handler(arguments, cancellationToken);
    }
}
