using FluentValidation;
using MediatR;
using POS.Domain.Common;

namespace POS.Application.Common.Behaviors;

public class ValidationBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : Result
{
  private readonly IEnumerable<IValidator<TRequest>> _validators;

  public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators) =>
      _validators = validators;

  public async Task<TResponse> Handle(
      TRequest request,
      RequestHandlerDelegate<TResponse> next,
      CancellationToken cancellationToken)
  {
    if (!_validators.Any())
    {
      return await next();
    }

    var errors = _validators
        .SelectMany(validator => validator.Validate(request).Errors)
        .Select(failure => new Error(failure.PropertyName, failure.ErrorMessage))
        .GroupBy(error => error.Code)
        .Select(group => new Error(
            group.Key,
            string.Join("; ", group.Select(item => item.Message).Distinct())))
        .ToArray();

    if (errors.Length > 0)
    {
      return CreateValidationResult<TResponse>(errors);
    }

    return await next();
  }

  private static TResult CreateValidationResult<TResult>(Error[] errors)
    where TResult : Result
  {
    if (typeof(TResult) == typeof(Result))
    {
      return (ValidationResult.WithErrors(errors) as TResult)!;
    }

    var resultType = typeof(TResult).GenericTypeArguments[0];
    var validationResult = typeof(ValidationResult<>)
        .MakeGenericType(resultType)
        .GetMethod(nameof(ValidationResult.WithErrors))!
        .Invoke(null, new object?[] { errors })!;

    return (TResult)validationResult;
  }
}
