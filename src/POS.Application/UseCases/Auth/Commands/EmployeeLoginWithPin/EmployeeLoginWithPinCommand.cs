using POS.Application.Abstractions.Messaging;
using POS.Application.UseCases.Auth.Dtos;

namespace POS.Application.UseCases.Auth.Commands.EmployeeLoginWithPin;

public record EmployeeLoginWithPinCommand(
    Guid StoreId,
    string Pin
) : ICommand<AuthDto>;