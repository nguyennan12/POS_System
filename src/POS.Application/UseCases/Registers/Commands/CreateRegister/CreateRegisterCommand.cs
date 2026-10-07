using System;
using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;
using POS.Contracts.V1.Registers;

namespace POS.Application.UseCases.Registers.Commands.CreateRegister;

public record CreateRegisterCommand(
    Guid StoreId,
    string Name,
    string Code
) : ICommand<RegisterResponse>, IRequirePermission
{
    public string RequiredPermission => "stores:update";
}
