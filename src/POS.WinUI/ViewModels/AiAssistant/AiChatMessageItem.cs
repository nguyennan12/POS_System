namespace POS.WinUI.ViewModels.AiAssistant;

public sealed record AiChatMessageItem(
    string Content,
    bool IsUser,
    string TimeLabel,
    bool IncludeInHistory = true,
    bool IsError = false,
    string? RetryPrompt = null);
