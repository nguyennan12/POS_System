using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using POS.Api.Auth;
using POS.Api.Mappings;
using POS.Application.UseCases.Payments.DTOs;
using POS.Application.UseCases.Payments.Queries.AuthorizePaymentSubscription;
using POS.Contracts.V1.Common;
using POS.Contracts.V1.Payments;
using POS.Domain.Common;

namespace POS.Api.Hubs;

[Authorize]
public sealed class PaymentHub(ISender sender, CurrentUser currentUser) : Hub
{
    public async Task<ApiResponse<PaymentStatusResponse>> SubscribePayment(Guid paymentId)
    {
        var authorized = await ReadSnapshotAsync(paymentId);
        if (authorized.IsFailure) return Failure(authorized);

        var group = PaymentHubGroups.ForPayment(paymentId);
        await Groups.AddToGroupAsync(Context.ConnectionId, group, Context.ConnectionAborted);
        try
        {
            // Join first, then read again: a webhook in between authorization and joining cannot be lost.
            var snapshot = await ReadSnapshotAsync(paymentId);
            if (snapshot.IsFailure)
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, group, CancellationToken.None);
                return Failure(snapshot);
            }

            var response = snapshot.Value!.ToResponse();
            var eventName = response.Status switch
            {
                "Success" => PaymentHubEvents.PaymentSuccess,
                "Failed" => PaymentHubEvents.PaymentFailed,
                _ => null
            };
            if (eventName is not null)
                await Clients.Caller.SendAsync(eventName, response, Context.ConnectionAborted);
            return ApiResponse<PaymentStatusResponse>.Ok(response);
        }
        catch
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, group, CancellationToken.None);
            throw;
        }
    }

    public Task UnsubscribePayment(Guid paymentId) =>
        Groups.RemoveFromGroupAsync(Context.ConnectionId, PaymentHubGroups.ForPayment(paymentId), Context.ConnectionAborted);

    private async Task<Result<PaymentStatusDto>> ReadSnapshotAsync(Guid paymentId)
    {
        // SignalR has its own invocation scope. Adapt its authenticated principal for the existing
        // ICurrentUser/AuthorizationBehavior even when no ambient HTTP request is available.
        var previous = currentUser.HubContext;
        try
        {
            currentUser.HubContext = Context.GetHttpContext();
            return await sender.Send(new AuthorizePaymentSubscriptionQuery(paymentId), Context.ConnectionAborted);
        }
        finally { currentUser.HubContext = previous; }
    }

    private static ApiResponse<PaymentStatusResponse> Failure(Result result) =>
        ApiResponse<PaymentStatusResponse>.Fail(new ApiError
        {
            Code = result.Error.Code, Message = result.Error.Message!, Type = result.Error.Type,
            ValidationErrors = result is IValidationResult validation
                ? validation.Errors.GroupBy(error => error.Code).ToDictionary(group => group.Key,
                    group => group.Select(error => error.Message ?? "Invalid value").Distinct().ToArray())
                : null
        });
}
