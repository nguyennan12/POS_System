
namespace POS.Domain.Common;

public interface IValidationResult
{
  public static readonly Error ValidationError = new(
      ErrorType.Validation,
      "ValidationError",
      "A validation problem occurred.");

  Error[] Errors { get; }
}
