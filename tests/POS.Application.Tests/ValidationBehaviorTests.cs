using FluentValidation;
using MediatR;
using POS.Application.Common.Behaviors;
using POS.Domain.Common;

namespace POS.Application.Tests;

public class ValidationBehaviorTests
{
    [Fact]
    public async Task Handle_WhenValidationFails_ReturnsValidationResult()
    {
        var behavior = new ValidationBehavior<TestRequest, Result<string>>(
            new IValidator<TestRequest>[] { new TestRequestValidator() });

        var result = await behavior.Handle(
            new TestRequest(string.Empty),
            () => Task.FromResult(Result<string>.Success("ok")),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.Error.Type);

        var validation = Assert.IsAssignableFrom<IValidationResult>(result);
        Assert.NotEmpty(validation.Errors);
        Assert.Equal("Name", validation.Errors[0].Code);
    }

    [Fact]
    public async Task Handle_WhenValidationPasses_ContinuesToNextHandler()
    {
        var behavior = new ValidationBehavior<TestRequest, Result<string>>(
            new IValidator<TestRequest>[] { new TestRequestValidator() });

        var result = await behavior.Handle(
            new TestRequest("Alice"),
            () => Task.FromResult(Result<string>.Success("ok")),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("ok", result.Value);
    }

    public sealed record TestRequest : IRequest<Result<string>>
    {
        public TestRequest(string name)
        {
            Name = name;
        }

        public string Name { get; private set; } = string.Empty;
    }

    public sealed class TestRequestValidator : AbstractValidator<TestRequest>
    {
        public TestRequestValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage("Name is required.");
        }
    }
}
