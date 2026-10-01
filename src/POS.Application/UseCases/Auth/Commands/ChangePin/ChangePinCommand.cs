using POS.Application.Abstractions.Messaging;

namespace POS.Application.UseCases.Auth.Commands.ChangePin;

public record ChangePinCommand(
    string OldPin,
    string NewPin
) : ICommand;
