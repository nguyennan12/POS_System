using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using POS.Contracts.V1.Common;
using Scalar.AspNetCore;

namespace POS.Api.Extensions;

public static class OpenApiExtensions
{
  public static IServiceCollection AddOpenApiDocumentation(this IServiceCollection services)
  {
    services.AddOpenApi(options =>
    {
      options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
      options.AddOperationTransformer(async (operation, context, cancellationToken) =>
          {
            // 1. Schema cho ApiResponse Envelope (lỗi)
          var errorSchema = await context.GetOrCreateSchemaAsync(typeof(ApiResponse<object>), null, cancellationToken);
          if (context.Document != null && errorSchema != null)
          {
            context.Document.AddComponent("ApiResponseError", errorSchema);
          }

          var exampleApiResponse = JsonNode.Parse("""
                {
                  "success": false,
                  "data": null,
                  "error": {
                    "code": "ERROR_CODE",
                    "message": "Detailed error message.",
                    "type": "Error Type",
                    "validationErrors": {
                      "field": ["Field is required."]
                    }
                  }
                }
                """);

          var apiResponseMediaType = new OpenApiMediaType
          {
            Schema = context.Document != null
                      ? new OpenApiSchemaReference("ApiResponseError", context.Document)
                      : null,
            Example = exampleApiResponse
          };

          var contentMediaTypes = new Dictionary<string, OpenApiMediaType>
          {
            ["application/json"] = apiResponseMediaType
          };

          operation.Responses ??= [];
          operation.Responses["400"] = new OpenApiResponse
          {
            Description = "Bad Request",
            Content = contentMediaTypes
          };
          operation.Responses["401"] = new OpenApiResponse
          {
            Description = "Unauthorized",
            Content = contentMediaTypes
          };
          operation.Responses["403"] = new OpenApiResponse
          {
            Description = "Forbidden",
            Content = contentMediaTypes
          };
          operation.Responses["404"] = new OpenApiResponse
          {
            Description = "Not Found",
            Content = contentMediaTypes
          };
          operation.Responses["500"] = new OpenApiResponse
          {
            Description = "Internal Server Error",
            Content = contentMediaTypes
          };
        });
    });

    return services;
  }

  public static WebApplication MapOpenApiDocumentation(this WebApplication app)
  {
    if (app.Environment.IsDevelopment())
    {
      app.MapOpenApi();
      app.MapScalarApiReference(options =>
      {
        options.WithTitle("POS API")
                     .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
      });
    }

    return app;
  }
}
