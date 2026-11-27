using POS.Domain.Common;

namespace POS.Application.UseCases.AI.Tools.Common;

public static class AiToolErrors
{
    public const string InvalidArguments = "INVALID_ARGUMENTS";
    public const string ToolNotFound = "TOOL_NOT_FOUND";
    public const string PermissionDenied = "PERMISSION_DENIED";
    public const string StoreRequired = "STORE_REQUIRED";
    public const string NotFound = "NOT_FOUND";
    public const string TooManyResults = "TOO_MANY_RESULTS";
    public const string QueryFailed = "QUERY_FAILED";
    public const string AiToolLoopLimit = "AI_TOOL_LOOP_LIMIT";

    public static AiToolExecutionResult InvalidArgument(string message = "Tham số tra cứu không hợp lệ.") =>
        AiToolExecutionResult.Fail(InvalidArguments, message);

    public static AiToolExecutionResult UnknownTool(string toolName) =>
        AiToolExecutionResult.Fail(ToolNotFound, $"Công cụ tra cứu '{toolName}' không được hỗ trợ.");

    public static AiToolExecutionResult Forbidden(string message = "Bạn không có quyền xem dữ liệu này.") =>
        AiToolExecutionResult.Fail(PermissionDenied, message);

    public static AiToolExecutionResult MissingStore(string message = "Vui lòng chọn chi nhánh làm việc trước khi tra cứu dữ liệu này.") =>
        AiToolExecutionResult.Fail(StoreRequired, message);

    public static AiToolExecutionResult ResourceNotFound(string message = "Không tìm thấy dữ liệu yêu cầu tại chi nhánh hiện tại.") =>
        AiToolExecutionResult.Fail(NotFound, message);

    public static AiToolExecutionResult ExceededResultLimit(string message = "Kết quả quá nhiều. Vui lòng thu hẹp điều kiện tìm kiếm.") =>
        AiToolExecutionResult.Fail(TooManyResults, message);

    public static AiToolExecutionResult ExecutionFailed(string message = "Không thể truy vấn dữ liệu lúc này. Vui lòng thử lại sau.") =>
        AiToolExecutionResult.Fail(QueryFailed, message);

    /// <summary>
    /// Chuyển đổi <see cref="Error"/> của tầng Domain/Application sang kết quả lỗi an toàn cho AI Tool,
    /// không làm lộ chi tiết kỹ thuật nội bộ.
    /// </summary>
    public static AiToolExecutionResult FromDomainError(Error error)
    {
        if (string.Equals(error.Code, "Store.Required", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(error.Code, "STORE.REQUIRED", StringComparison.OrdinalIgnoreCase) ||
            error.Code.EndsWith(".StoreRequired", StringComparison.OrdinalIgnoreCase))
        {
            return MissingStore();
        }

        if (string.Equals(error.Code, "Store.AccessDenied", StringComparison.OrdinalIgnoreCase) ||
            error.Code.EndsWith(".StoreAccessDenied", StringComparison.OrdinalIgnoreCase) ||
            error.Type is ErrorType.Forbidden or ErrorType.Unauthorized)
        {
            return Forbidden();
        }

        return error.Type switch
        {
            ErrorType.NotFound => ResourceNotFound(),
            ErrorType.Validation or ErrorType.Invalid => InvalidArgument(
                !string.IsNullOrWhiteSpace(error.Message) && error.Message.Length <= 200
                    ? error.Message
                    : "Tham số hoặc điều kiện tra cứu không hợp lệ."),
            _ => ExecutionFailed()
        };
    }
}
