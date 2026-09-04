using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace POS.Api.Extensions;

internal sealed class BearerSecuritySchemeTransformer(
    IAuthenticationSchemeProvider authenticationSchemeProvider)
    : IOpenApiDocumentTransformer
{
  public async Task TransformAsync(
      OpenApiDocument document,
      OpenApiDocumentTransformerContext context,
      CancellationToken cancellationToken)
  {
    var authenticationSchemes = await authenticationSchemeProvider.GetAllSchemesAsync();

    if (!authenticationSchemes.Any(authScheme => authScheme.Name == "Bearer"))
      return;

    var securitySchemes = new Dictionary<string, IOpenApiSecurityScheme>
    {
      ["Bearer"] = new OpenApiSecurityScheme
      {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        In = ParameterLocation.Header,
        BearerFormat = "JWT",
        Description = "Enter JWT Bearer token"
      }
    };

    document.Components ??= new OpenApiComponents();
    document.Components.SecuritySchemes = securitySchemes;

    if (document.Paths != null)
    {
      foreach (var path in document.Paths.Values)
      {
        if (path.Operations == null) continue;

        foreach (var operation in path.Operations.Values)
        {
          if (operation == null) continue;

          operation.Security ??= [];
          operation.Security.Add(new OpenApiSecurityRequirement
          {
            [new OpenApiSecuritySchemeReference("Bearer", document)] = []
          });
        }
      }
    }
  }
}
