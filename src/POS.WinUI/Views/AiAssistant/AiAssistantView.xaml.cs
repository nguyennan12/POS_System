using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using POS.WinUI.ViewModels.AiAssistant;

namespace POS.WinUI.Views.AiAssistant;

public partial class AiAssistantView : UserControl
{
    private AiAssistantViewModel? _subscribedViewModel;

    public AiAssistantView()
    {
        InitializeComponent();
        DataContext = App.Services.GetRequiredService<AiAssistantViewModel>();
        DataContextChanged += OnDataContextChanged;
        Loaded += (_, _) => SubscribeToMessages();
        Unloaded += (_, _) => UnsubscribeFromMessages();
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        UnsubscribeFromMessages();
        if (IsLoaded)
            SubscribeToMessages();
    }

    private void SubscribeToMessages()
    {
        if (DataContext is not AiAssistantViewModel viewModel)
            return;

        if (ReferenceEquals(_subscribedViewModel, viewModel))
        {
            ScrollToLatestMessage();
            return;
        }

        UnsubscribeFromMessages();
        _subscribedViewModel = viewModel;
        viewModel.Messages.CollectionChanged += OnMessagesChanged;
        ScrollToLatestMessage();
    }

    private void UnsubscribeFromMessages()
    {
        if (_subscribedViewModel is not null)
            _subscribedViewModel.Messages.CollectionChanged -= OnMessagesChanged;
        _subscribedViewModel = null;
    }

    private void OnMessagesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(ScrollToLatestMessage));
    }

    private void ScrollToLatestMessage()
    {
        if (DataContext is AiAssistantViewModel { IsChatOpen: true })
            MessagesScrollViewer.ScrollToEnd();
    }

    private void OnMessageInputPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter)
            return;

        if ((Keyboard.Modifiers & ModifierKeys.Shift) != 0)
            return;

        e.Handled = true;
        if (DataContext is AiAssistantViewModel viewModel
            && viewModel.SendMessageCommand.CanExecute(null))
        {
            viewModel.SendMessageCommand.Execute(null);
        }
    }
}
