using System.Text.Json;
using MediatR;
using POS.Application.Abstractions.AI;
using POS.Application.UseCases.StoreLayouts;
using POS.Application.UseCases.StoreLayouts.Queries.PreviewPickingRoute;

namespace POS.Application.UseCases.AI.Tools.StoreLayouts;

public class PreviewPickingRouteToolHandler : IAiToolHandler
{
    private readonly ISender _sender;

    public PreviewPickingRouteToolHandler(ISender sender)
    {
        _sender = sender;
    }

    public string ToolName => "preview_picking_route";

    public string Description => "Tính toán và xem trước tuyến đường di chuyển tối ưu trong cửa hàng để lấy danh sách sản phẩm cần nhặt (Smart Picking).";

    public string InputSchemaJson => """
        {
          "type": "object",
          "properties": {
            "items": {
              "type": "array",
              "minItems": 1,
              "maxItems": 50,
              "items": {
                "type": "object",
                "properties": {
                  "skuId": { "type": "string", "format": "uuid" },
                  "quantity": { "type": "number", "exclusiveMinimum": 0 }
                },
                "required": ["skuId", "quantity"],
                "additionalProperties": false
              }
            }
          },
          "required": ["items"],
          "additionalProperties": false
        }
        """;

    public async Task<string> ExecuteAsync(string inputJson, CancellationToken cancellationToken = default)
    {
        try
        {
            using var doc = JsonDocument.Parse(inputJson);
            if (!doc.RootElement.TryGetProperty("items", out var itemsElement) || itemsElement.ValueKind != JsonValueKind.Array)
            {
                return JsonSerializer.Serialize(new { error = "INVALID_ARGUMENTS", message = "Danh sách items không hợp lệ." });
            }

            var items = new List<PickingRouteItemDto>();
            foreach (var item in itemsElement.EnumerateArray())
            {
                if (item.TryGetProperty("skuId", out var skuIdProp) && Guid.TryParse(skuIdProp.GetString(), out var skuId) &&
                    item.TryGetProperty("quantity", out var qtyProp) && qtyProp.TryGetDecimal(out var qty) && qty > 0)
                {
                    items.Add(new PickingRouteItemDto(skuId, qty));
                }
            }

            if (items.Count == 0)
            {
                return JsonSerializer.Serialize(new { error = "INVALID_ARGUMENTS", message = "Cần ít nhất một item hợp lệ để tính toán tuyến đường." });
            }

            var result = await _sender.Send(new PreviewPickingRouteQuery(items), cancellationToken);
            if (result.IsFailure)
            {
                return JsonSerializer.Serialize(new { error = result.Error.Code, message = result.Error.Message });
            }

            return JsonSerializer.Serialize(result.Value);
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = "EXECUTION_ERROR", message = ex.Message });
        }
    }
}
