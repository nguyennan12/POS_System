
using MediatR;
using POS.Domain.Common;

namespace POS.Application.Abstractions.Messaging
{
  public interface IQuery<TResponse>
    : IRequest<Result<TResponse>>
  {
  }

}
