using System.Threading.Tasks;

namespace POS.WinUI.Core.Services;

public enum ConfirmDialogVariant
{
    Danger,   // Xóa vĩnh viễn, hủy dữ liệu quan trọng (Đỏ)
    Warning,  // Cảnh báo, lưu đè, đóng ca (Cam)
    Primary   // Xác nhận thông thường, in ấn, đồng bộ (Cam/Xanh)
}

public sealed class ConfirmDialogOptions
{
    public string Title { get; set; } = "Xác nhận hành động";
    public string? Subtitle { get; set; } = "Hành động này không thể hoàn tác";
    public string Message { get; set; } = "Bạn có chắc chắn muốn thực hiện hành động này?";
    public string? WarningMessage { get; set; }
    public string ConfirmText { get; set; } = "Xác nhận";
    public string CancelText { get; set; } = "Hủy bỏ";
    public ConfirmDialogVariant Variant { get; set; } = ConfirmDialogVariant.Danger;
}

public interface IConfirmDialogService
{
    Task<bool> ShowConfirmAsync(ConfirmDialogOptions options);

    Task<bool> ShowDangerConfirmAsync(string title, string message, string? warningMessage = null, string confirmText = "Xóa vĩnh viễn");
}
