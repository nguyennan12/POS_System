using System;
using System.Collections.Generic;
using System.Text;

namespace POS.Domain.Common
{
  public enum ErrorType
  {
    None,
    Validation,
    NotFound,
    AlreadyExists,
    Invalid,
    Unauthorized,
    Forbidden,
    Unexpected
  }

  public record Error(string Code, string? Message = null)
  {
    public ErrorType Type { get; init; } = ErrorType.Unexpected;

    public Error(ErrorType type, string code, string? message = null)
        : this(code, message)
    {
      Type = type;
    }

    public static readonly Error None = new(ErrorType.None, string.Empty);

    public static implicit operator Result(Error error) => Result.Failure(error);
  }
}
