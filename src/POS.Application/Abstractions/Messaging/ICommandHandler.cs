using System;
using System.Collections.Generic;
using System.Text;
using POS.Domain.Common;

namespace POS.Application.Abstractions.Messaging
{

  public interface ICommandHandler<in TCommand>
      where TCommand : ICommand
  {
    Task<Result> Handle(TCommand command, CancellationToken cancellationToken);
  }

  public interface ICommandHandler<in TCommand, TResponse>
      where TCommand : ICommand<TResponse>
  {
    Task<Result<TResponse>> Handle(TCommand command, CancellationToken cancellationToken);
  }

}
