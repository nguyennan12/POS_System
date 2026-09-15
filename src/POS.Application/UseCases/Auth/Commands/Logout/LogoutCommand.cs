

using POS.Application.Abstractions.Messaging;

namespace POS.Application.UseCases.Auth.Commands.Logout;

public record LogoutCommand(string RefreshToken) : ICommand;