using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using POS.Api.Extensions;
using POS.Api.Mappings;
using POS.Application.UseCases.Categories.Commands.CreateCategory;
using POS.Application.UseCases.Categories.Commands.UpdateCategory;
using POS.Application.UseCases.Categories.Commands.DeleteCategory;
using POS.Application.UseCases.Categories.Queries.GetCategoriesTree;
using POS.Contracts.V1.Categories;
using POS.Contracts.V1.Common;

namespace POS.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/categories")]
public class CategoriesController(ISender mediator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<CategoryResponse>>>> GetTree(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetCategoriesTreeQuery(), cancellationToken);

        if (result.IsFailure)
            return this.ToActionResult(result);

        return Ok(ApiResponse<List<CategoryResponse>>.Ok(result.Value!.Select(c => c.ToResponse()).ToList()));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<CategoryResponse>>> Create(
        [FromBody] CreateCategoryRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateCategoryCommand(
            request.Name,
            request.ParentId,
            request.DisplayOrder,
            request.ImageUrl,
            request.IsVisible);

        var result = await mediator.Send(command, cancellationToken);

        if (result.IsFailure)
            return this.ToActionResult(result);

        return Ok(ApiResponse<CategoryResponse>.Ok(result.Value!.ToResponse()));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<CategoryResponse>>> Update(
        Guid id,
        [FromBody] UpdateCategoryRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateCategoryCommand(
            id,
            request.Name,
            request.ParentId,
            request.DisplayOrder,
            request.ImageUrl,
            request.IsVisible);

        var result = await mediator.Send(command, cancellationToken);

        if (result.IsFailure)
            return this.ToActionResult(result);

        return Ok(ApiResponse<CategoryResponse>.Ok(result.Value!.ToResponse()));
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new DeleteCategoryCommand(id), cancellationToken);

        if (result.IsFailure)
            return this.ToActionResult(result);

        return Ok(ApiResponse<bool>.Ok(true));
    }
}
