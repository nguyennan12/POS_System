using POS.Application.Abstractions.Messaging;
using POS.Application.UseCases.Categories.Dtos;

namespace POS.Application.UseCases.Categories.Queries.GetCategoriesTree;

public record GetCategoriesTreeQuery : IQuery<List<CategoryDto>>;
