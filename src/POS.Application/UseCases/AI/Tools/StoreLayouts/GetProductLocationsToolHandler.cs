using System.Text.Json;
using MediatR;
using POS.Application.Abstractions.AI;
using POS.Application.UseCases.StoreLayouts.Queries.GetProductLocations;

namespace POS.Application.UseCases.AI.Tools.StoreLayouts;

public class GetProductLocationsToolHandler : IAiToolHandler
{
    private readonly ISender _sender;

    public GetProductLocationsToolHandler(ISender sender)
    {
        _sender = sender;
    }

    public string ToolName => "get_product_locations";

    public string Description => "Tra cứu vị trí trưng bày của sản phẩm (SKU) trên sơ đồ kệ hàng đang hoạt động của cửa hàng hiện tại.";

    public string InputSchemaJson => """
        {
          "type": "object",
          "properties": {
            "skuId": { "type": "string", "format": "uuid", "description": "Mã định danh duy nhất của SKU" }
          },
          "required": ["skuId"],
          "additionalProperties": false
        }
        """;

    public async Task<string> ExecuteAsync(string inputJson, CancellationToken cancellationToken = default)
    {
        try
        {
            using var doc = JsonDocument.Parse(inputJson);
            if (!doc.RootElement.TryGetProperty("skuId", out var skuIdElement) ||
                !Guid.TryParse(skuIdElement.GetString(), out var skuId))
            {
                return JsonSerializer.Serialize(new { error = "INVALID_ARGUMENTS", message = "skuId không hợp lệ hoặc thiếu định dạng UUID." });
            }

            var result = await _sender.Send(new GetProductLocationsQuery(skuId), cancellationToken);
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
