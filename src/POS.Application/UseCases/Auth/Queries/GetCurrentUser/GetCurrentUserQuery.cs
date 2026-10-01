using POS.Application.Abstractions.Messaging;
using POS.Application.UseCases.Auth.Dtos;

namespace POS.Application.UseCases.Auth.Queries.GetCurrentUser;

public record GetCurrentUserQuery : IQuery<CurrentUserDto>;
