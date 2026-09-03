using System;
using System.Collections.Generic;
using System.Text;
using MediatR;
using POS.Domain.Common;

namespace POS.Application.Abstractions.Messaging
{
  public interface IQueryHandler<in TQuery, TResponse> : IRequestHandler<TQuery, Result<TResponse>>
  where TQuery : IQuery<TResponse>
  {
    new Task<Result<TResponse>> Handle(TQuery query, CancellationToken cancellationToken);
  }
}
