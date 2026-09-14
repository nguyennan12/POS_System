using POS.Application.Abstractions.Messaging;
using POS.Application.UseCases.Auth.Dtos;

namespace POS.Application.UseCases.Auth.Commands.Refresh;

public record RefreshTokenCommand(string RefreshToken) : ICommand<AuthDto>; 