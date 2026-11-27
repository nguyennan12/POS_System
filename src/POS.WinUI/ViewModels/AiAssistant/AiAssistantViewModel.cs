using System.Net.Http;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using POS.Contracts.V1.Chatbot;
using POS.WinUI.Core.ApiClients;
using POS.WinUI.Core.Services;
using POS.WinUI.ViewModels.Common;
using Wpf.Ui.Controls;

namespace POS.WinUI.ViewModels.AiAssistant;

public sealed partial class AiAssistantViewModel : ManagementTabViewModelBase
{
    private readonly AiAssistantApiClient _apiClient;
    private readonly SessionService _sessionService;
    private long _conversationVersion;

    public override string TabId => "AiAssistant";
    public override string Title => Loc["ai.title"];
    public override string Subtitle => Loc["ai.subtitle"];
    public override SymbolRegular Icon => SymbolRegular.Chat24;

    public ObservableCollection<AiChatMessageItem> Messages { get; } = new();

    [ObservableProperty]
    private string _messageDraft = string.Empty;

    [ObservableProperty]
    private bool _isSending;

    [ObservableProperty]
    private bool _isChatOpen;

    [ObservableProperty]
    private string _statusMessage = "";

    public bool CanSend => !IsSending && !string.IsNullOrWhiteSpace(MessageDraft);

    public AiAssistantViewModel(AiAssistantApiClient apiClient, SessionService sessionService)
        : base(sessionService)
    {
        _apiClient = apiClient;
        _sessionService = sessionService;
        _sessionService.SessionChanged += OnSessionChanged;
        AddWelcomeMessage();
    }

    partial void OnMessageDraftChanged(string value) => SendMessageCommand.NotifyCanExecuteChanged();

    partial void OnIsSendingChanged(bool value) => SendMessageCommand.NotifyCanExecuteChanged();

    [RelayCommand(CanExecute = nameof(CanSend))]
    private async Task SendMessageAsync()
    {
        var message = MessageDraft.Trim();
        if (message.Length == 0) return;

        var conversationVersion = _conversationVersion;

        var history = Messages
            .Where(item => item.IncludeInHistory)
            .TakeLast(20)
            .Select(item => new AiChatTurnRequest(item.IsUser ? "user" : "assistant", item.Content))
            .ToArray();

        MessageDraft = string.Empty;
        StatusMessage = "";
        AddMessage(message, isUser: true);
        IsSending = true;

        try
        {
            var response = await _apiClient.AskAsync(new AskAiRequest(message, history));
            if (conversationVersion == _conversationVersion)
                AddMessage(response.Reply, isUser: false);
        }
        catch (Exception exception)
        {
            if (conversationVersion == _conversationVersion)
            {
                AddErrorMessage(GetFriendlyError(exception), message);
            }
        }
        finally
        {
            if (conversationVersion == _conversationVersion)
                IsSending = false;
        }
    }

    [RelayCommand]
    private void ClearConversation()
    {
        _conversationVersion++;
        Messages.Clear();
        StatusMessage = "";
        IsSending = false;
        Messages.Add(new AiChatMessageItem(
            "Cuộc trò chuyện đã được làm mới. Bạn muốn hỏi gì?",
            false,
            DateTime.Now.ToString("HH:mm")));
    }

    [RelayCommand]
    private void ToggleChat() => IsChatOpen = !IsChatOpen;

    [RelayCommand]
    private void CloseChat() => IsChatOpen = false;

    [RelayCommand]
    private async Task RetryMessageAsync(AiChatMessageItem? errorMessage)
    {
        if (IsSending || errorMessage is not { IsError: true, RetryPrompt: { Length: > 0 } prompt })
            return;

        var errorIndex = Messages.IndexOf(errorMessage);
        if (errorIndex < 0)
            return;

        Messages.RemoveAt(errorIndex);
        if (errorIndex > 0 && Messages[errorIndex - 1] is { IsUser: true } previousUser
                            && string.Equals(previousUser.Content, prompt, StringComparison.Ordinal))
        {
            Messages.RemoveAt(errorIndex - 1);
        }

        MessageDraft = prompt;
        await SendMessageAsync();
    }

    private void OnSessionChanged()
    {
        _conversationVersion++;
        IsChatOpen = false;
        MessageDraft = string.Empty;
        StatusMessage = "";
        IsSending = false;
        Messages.Clear();
        AddWelcomeMessage();
    }

    private void AddWelcomeMessage()
    {
        Messages.Add(new AiChatMessageItem(
            "Xin chào! Tôi có thể tra cứu nhanh sản phẩm, tồn kho, đơn hàng, hóa đơn, khách hàng, báo cáo, khuyến mãi của chi nhánh hiện tại và hướng dẫn sử dụng POS.",
            false,
            DateTime.Now.ToString("HH:mm"),
            IncludeInHistory: false));
    }

    private void AddMessage(string content, bool isUser)
    {
        Messages.Add(new AiChatMessageItem(content, isUser, DateTime.Now.ToString("HH:mm")));
        while (Messages.Count > 40)
            Messages.RemoveAt(0);
    }

    private void AddErrorMessage(string content, string retryPrompt)
    {
        Messages.Add(new AiChatMessageItem(
            content,
            IsUser: false,
            DateTime.Now.ToString("HH:mm"),
            IncludeInHistory: false,
            IsError: true,
            RetryPrompt: retryPrompt));
        while (Messages.Count > 40)
            Messages.RemoveAt(0);
    }

    private static string GetFriendlyError(Exception exception) => exception switch
    {
        TaskCanceledException => "Yêu cầu phản hồi quá lâu. Vui lòng kiểm tra kết nối rồi thử lại.",
        HttpRequestException => "Không kết nối được tới máy chủ. Vui lòng kiểm tra mạng rồi thử lại.",
        InvalidOperationException when !string.IsNullOrWhiteSpace(exception.Message) => exception.Message,
        _ => "Trợ lý AI chưa phản hồi được. Vui lòng thử lại sau."
    };

    public override void Dispose()
    {
        _sessionService.SessionChanged -= OnSessionChanged;
        base.Dispose();
    }
}
