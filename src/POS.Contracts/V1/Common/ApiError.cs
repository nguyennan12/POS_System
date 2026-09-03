using POS.Domain.Common;

namespace POS.Contracts.V1.Common;

public class ApiError
{
    public string Code { get; set; } = "INTERNAL_ERROR";
    public string Message { get; set; } = "Đã có lỗi xảy ra.";
    public ErrorType Type { get; set; } = ErrorType.Unexpected;
    public Dictionary<string, string[]>? ValidationErrors { get; set; }
}
