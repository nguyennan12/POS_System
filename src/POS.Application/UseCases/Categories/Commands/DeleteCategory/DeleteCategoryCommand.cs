using POS.Application.Abstractions.Messaging;

namespace POS.Application.UseCases.Categories.Commands.DeleteCategory;

public record DeleteCategoryCommand(Guid Id) : ICommand;
