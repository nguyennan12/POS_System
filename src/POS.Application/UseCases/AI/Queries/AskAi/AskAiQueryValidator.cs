using FluentValidation;

namespace POS.Application.UseCases.AI.Queries.AskAi;

public sealed class AskAiQueryValidator : AbstractValidator<AskAiQuery>
{
    private const int MaxMessageLength = 4000;
    private const int MaxHistoryTurns = 20;

    public AskAiQueryValidator()
    {
        RuleFor(query => query.Message)
            .NotEmpty().WithMessage("Nội dung câu hỏi không được để trống.")
            .MaximumLength(MaxMessageLength).WithMessage("Câu hỏi không được vượt quá 4.000 ký tự.");

        RuleFor(query => query.History)
            .NotNull()
            .Must(history => history.Count <= MaxHistoryTurns)
            .WithMessage("Lịch sử hội thoại vượt quá giới hạn.");

        RuleForEach(query => query.History).ChildRules(turn =>
        {
            turn.RuleFor(item => item.Role)
                .Must(role => role is "user" or "assistant")
                .WithMessage("Vai trò tin nhắn không hợp lệ.");
            turn.RuleFor(item => item.Content)
                .NotEmpty()
                .MaximumLength(MaxMessageLength);
        });
    }
}
