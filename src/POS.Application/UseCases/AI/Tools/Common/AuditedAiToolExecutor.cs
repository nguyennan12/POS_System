using System.Diagnostics;
using Microsoft.Extensions.Logging;
using POS.Application.Abstractions.AI;
using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Persistence;
using POS.Domain.Auditing;

namespace POS.Application.UseCases.AI.Tools.Common;

/// <summary>
/// Decorator bọc ngoài <see cref="AiToolExecutor"/> để ghi nhật ký thực thi công cụ AI vào bảng <c>audit_logs</c>.
/// Áp dụng chính sách best-effort: nếu ghi audit gặp sự cố lưu trữ, ghi log cảnh báo nội bộ nhưng không làm hỏng kết quả tra cứu hợp lệ.
/// Tuyệt đối không lưu prompt, tham số đầu vào, PII hoặc dữ liệu kết quả vào <c>Description</c>.
/// </summary>
public sealed class AuditedAiToolExecutor(
    AiToolExecutor innerExecutor,
    ICurrentUser currentUser,
    IAuditLogRepository auditLogRepository,
    IUnitOfWork unitOfWork,
    ILogger<AuditedAiToolExecutor> logger) : IAiToolExecutor
{
    public async Task<AiToolResult> ExecuteAsync(
        AiToolCall call,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var toolResult = await innerExecutor.ExecuteAsync(call, cancellationToken);
        stopwatch.Stop();

        await TryWriteAuditAsync(toolResult, stopwatch.ElapsedMilliseconds, cancellationToken);

        return toolResult;
    }

    private async Task TryWriteAuditAsync(
        AiToolResult toolResult,
        long durationMs,
        CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated && currentUser.EmployeeId is null && currentUser.StoreId is null)
            return;

        try
        {
            var auditEntry = AuditLog.AiToolExecution(
                executionId: Guid.NewGuid(),
                employeeId: currentUser.EmployeeId,
                storeId: currentUser.StoreId,
                toolName: toolResult.Name,
                success: toolResult.Result.Success,
                durationMs: durationMs,
                resultCount: toolResult.Result.ResultCount,
                errorCode: toolResult.Result.ErrorCode);

            await auditLogRepository.AddAsync(auditEntry, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogWarning(
                exception,
                "Failed to persist audit log for AI tool '{ToolName}'.",
                toolResult.Name);
        }
    }
}
