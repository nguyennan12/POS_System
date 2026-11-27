using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using POS.Application.Abstractions.AI;
using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Persistence;
using POS.Application.UseCases.AI.Queries.AskAi;
using POS.Application.UseCases.AI.Tools.Common;
using POS.Domain.Auditing;

namespace POS.Application.Tests.AI;

public sealed class AiOrchestrationAndAuditTests
{
    [Fact]
    public async Task AuditedExecutor_PersistsMetadataOnly_ForSuccessAndFailure_WithoutPiiOrPayload()
    {
        var employeeId = Guid.NewGuid();
        var storeId = Guid.NewGuid();
        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.IsAuthenticated.Returns(true);
        currentUser.EmployeeId.Returns(employeeId);
        currentUser.StoreId.Returns(storeId);

        var capturedLogs = new List<AuditLog>();
        var auditRepo = Substitute.For<IAuditLogRepository>();
        auditRepo
            .AddAsync(Arg.Do<AuditLog>(log => capturedLogs.Add(log)), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var unitOfWork = Substitute.For<IUnitOfWork>();
        unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

        var searchHandler = new DelegateHandler(
            AiToolNames.SearchProducts,
            (_, _) => Task.FromResult(AiToolExecutionResult.Ok(new[] { new { phone = "0909123456" } }, 3)));
        var deniedHandler = new DelegateHandler(
            AiToolNames.GetRevenueReport,
            (_, _) => Task.FromResult(AiToolErrors.Forbidden()));

        var inner = new AiToolExecutor([searchHandler, deniedHandler], NullLogger<AiToolExecutor>.Instance);
        var audited = new AuditedAiToolExecutor(
            inner,
            currentUser,
            auditRepo,
            unitOfWork,
            NullLogger<AuditedAiToolExecutor>.Instance);

        using var argsDoc = JsonDocument.Parse("""{"keyword":"0909123456_secret_keyword"}""");

        var okResult = await audited.ExecuteAsync(
            new AiToolCall("provider-call-1", AiToolNames.SearchProducts, argsDoc.RootElement),
            CancellationToken.None);
        var failResult = await audited.ExecuteAsync(
            new AiToolCall("provider-call-2", AiToolNames.GetRevenueReport, argsDoc.RootElement),
            CancellationToken.None);

        okResult.IsError.Should().BeFalse();
        failResult.IsError.Should().BeTrue();

        capturedLogs.Should().HaveCount(2);

        capturedLogs[0].EntityType.Should().Be("AiTool");
        capturedLogs[0].EntityId.Should().NotBeEmpty();
        capturedLogs[0].EmployeeId.Should().Be(employeeId);
        capturedLogs[0].StoreId.Should().Be(storeId);
        capturedLogs[0].Action.Should().Be(AiToolNames.SearchProducts);
        capturedLogs[0].Description.Should().StartWith("Status=Success; DurationMs=");
        capturedLogs[0].Description.Should().EndWith("; ResultCount=3");
        capturedLogs[0].Description.Should().NotContain("0909123456");
        capturedLogs[0].Description.Should().NotContain("secret_keyword");

        capturedLogs[1].Action.Should().Be(AiToolNames.GetRevenueReport);
        capturedLogs[1].Description.Should().StartWith("Status=Failure; DurationMs=");
        capturedLogs[1].Description.Should().EndWith("; ErrorCode=PERMISSION_DENIED");

        await unitOfWork.Received(2).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AuditedExecutor_DoesNotFailBusinessRead_WhenAuditSaveThrows()
    {
        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.IsAuthenticated.Returns(true);
        currentUser.EmployeeId.Returns(Guid.NewGuid());

        var auditRepo = Substitute.For<IAuditLogRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        unitOfWork
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns<Task<int>>(_ => throw new InvalidOperationException("Audit DB transient error"));

        var inner = new AiToolExecutor(
            [new DelegateHandler(AiToolNames.SearchProducts, (_, _) => Task.FromResult(AiToolExecutionResult.EmptyList()))],
            NullLogger<AiToolExecutor>.Instance);
        var audited = new AuditedAiToolExecutor(
            inner,
            currentUser,
            auditRepo,
            unitOfWork,
            NullLogger<AuditedAiToolExecutor>.Instance);

        using var argsDoc = JsonDocument.Parse("{}");
        var result = await audited.ExecuteAsync(
            new AiToolCall("call_1", AiToolNames.SearchProducts, argsDoc.RootElement),
            CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task AskAiHandler_ExecutesChainedSearchAndInventoryTools_PreservingCallIds_AndReturnsFinalAnswer()
    {
        var skuId = Guid.NewGuid();
        var capturedRequests = new List<AiGenerationRequest>();
        var modelClient = Substitute.For<IAiModelClient>();

        using var round1Args = JsonDocument.Parse("""{"keyword":"Coca 330ml"}""");
        using var round2Args = JsonDocument.Parse($$"""{"skuId":"{{skuId}}"}""");

        var callStep = 0;
        modelClient
            .GenerateAsync(Arg.Any<AiGenerationRequest>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var req = callInfo.Arg<AiGenerationRequest>();
                capturedRequests.Add(new AiGenerationRequest(req.SystemInstruction, req.Messages.ToArray(), req.Tools));
                callStep++;

                return callStep switch
                {
                    1 => Task.FromResult(AiGenerationResponse.RequiresAction(
                        [new AiToolCall("call_search_1", AiToolNames.SearchProducts, round1Args.RootElement)])),
                    2 => Task.FromResult(AiGenerationResponse.RequiresAction(
                        [new AiToolCall("call_inv_2", AiToolNames.GetInventory, round2Args.RootElement)])),
                    _ => Task.FromResult(AiGenerationResponse.Completed("Coca 330ml hiện còn 36 lon tại cửa hàng."))
                };
            });

        var executor = Substitute.For<IAiToolExecutor>();
        executor
            .ExecuteAsync(Arg.Any<AiToolCall>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var call = callInfo.Arg<AiToolCall>();
                return call.Name switch
                {
                    AiToolNames.SearchProducts => Task.FromResult(new AiToolResult(
                        call.Id,
                        call.Name,
                        AiToolExecutionResult.Ok(new[] { new { skuId, productName = "Coca 330ml" } }, 1),
                        IsError: false)),
                    AiToolNames.GetInventory => Task.FromResult(new AiToolResult(
                        call.Id,
                        call.Name,
                        AiToolExecutionResult.Ok(new { skuId, qtyOnHand = 36m }, 1),
                        IsError: false)),
                    _ => throw new InvalidOperationException("Unexpected tool")
                };
            });

        var handler = new AskAiQueryHandler(modelClient, new AiToolRegistry(), executor);

        var result = await handler.Handle(
            new AskAiQuery("Coca 330ml còn bao nhiêu?", []),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("Coca 330ml hiện còn 36 lon tại cửa hàng.");

        capturedRequests.Should().HaveCount(3);
        capturedRequests[0].Tools.Should().HaveCount(22);

        var finalTurnList = capturedRequests[2].Messages;
        finalTurnList.Should().HaveCount(5);
        finalTurnList[1].Role.Should().Be("assistant");
        finalTurnList[1].ToolCalls![0].Id.Should().Be("call_search_1");
        finalTurnList[2].Role.Should().Be("tool");
        finalTurnList[2].ToolCallId.Should().Be("call_search_1");
        finalTurnList[3].Role.Should().Be("assistant");
        finalTurnList[3].ToolCalls![0].Id.Should().Be("call_inv_2");
        finalTurnList[4].Role.Should().Be("tool");
        finalTurnList[4].ToolCallId.Should().Be("call_inv_2");
    }

    [Fact]
    public async Task AskAiHandler_StopsAtFourthRequiresActionRound_WithAiToolLoopLimit()
    {
        using var argsDoc = JsonDocument.Parse("""{"keyword":"test"}""");
        var modelClient = Substitute.For<IAiModelClient>();
        modelClient
            .GenerateAsync(Arg.Any<AiGenerationRequest>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult(AiGenerationResponse.RequiresAction(
                [new AiToolCall($"call_{Guid.NewGuid():N}", AiToolNames.SearchProducts, argsDoc.RootElement)])));

        var toolExecutionCount = 0;
        var executor = Substitute.For<IAiToolExecutor>();
        executor
            .ExecuteAsync(Arg.Any<AiToolCall>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                toolExecutionCount++;
                var call = callInfo.Arg<AiToolCall>();
                return Task.FromResult(new AiToolResult(call.Id, call.Name, AiToolExecutionResult.EmptyList(), false));
            });

        var handler = new AskAiQueryHandler(modelClient, new AiToolRegistry(), executor);

        var result = await handler.Handle(new AskAiQuery("Lặp vô hạn", []), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(AiToolErrors.AiToolLoopLimit);
        toolExecutionCount.Should().Be(3); // Chỉ thực thi tối đa 3 vòng; vòng thứ 4 bị chặn trước khi gọi tool
    }

    private sealed class DelegateHandler(
        string name,
        Func<JsonElement, CancellationToken, Task<AiToolExecutionResult>> execute) : IAiToolHandler
    {
        public string Name { get; } = name;

        public Task<AiToolExecutionResult> ExecuteAsync(JsonElement arguments, CancellationToken cancellationToken) =>
            execute(arguments, cancellationToken);
    }
}
