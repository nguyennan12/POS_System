using POS.Application.Abstractions.Messaging;
using POS.Application.UseCases.Auth.Dtos;

namespace POS.Application.UseCases.Auth.Commands.EmployeeLoginWithPassword;

public record EmployeeLoginWithPasswordCommand(
    string Username,
    string Password
) : ICommand<AuthDto>;
