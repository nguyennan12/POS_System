using POS.Application.Abstractions.Messaging;

namespace POS.Application.UseCases.Auth.Commands.ChangePassword;

public record ChangePasswordCommand(
    string OldPassword,
    string NewPassword
) : ICommand;
