using System.Threading.Tasks;
using System.Windows;
using POS.WinUI.Views.Common;

namespace POS.WinUI.Core.Services;

public sealed class ConfirmDialogService : IConfirmDialogService
{
    public Task<bool> ShowConfirmAsync(ConfirmDialogOptions options)
    {
        var tcs = new TaskCompletionSource<bool>();

        Application.Current?.Dispatcher?.Invoke(() =>
        {
            var dialog = new ConfirmDialogWindow(options);
            dialog.ShowDialog();
            tcs.SetResult(dialog.Confirmed);
        });

        return tcs.Task;
    }

    public Task<bool> ShowDangerConfirmAsync(string title, string message, string? warningMessage = null, string confirmText = "Xóa vĩnh viễn")
    {
        return ShowConfirmAsync(new ConfirmDialogOptions
        {
            Title = title,
            Subtitle = "Hành động này không thể hoàn tác",
            Message = message,
            WarningMessage = warningMessage,
            ConfirmText = confirmText,
            CancelText = "Hủy bỏ",
            Variant = ConfirmDialogVariant.Danger
        });
    }
}
