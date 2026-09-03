using System;
using System.Collections.Generic;
using System.Text;
using MediatR;
using POS.Domain.Common;

namespace POS.Application.Abstractions.Messaging
{

  public interface ICommand : IRequest<Result>
  {
  }

  public interface ICommand<TResponse> : IRequest<Result<TResponse>>
  {
  }


}
