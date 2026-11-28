using System.Net.Http;
using POS.WinUI.Core.Services;

namespace POS.WinUI.Core.ApiClients;

public sealed record ScannerDeviceResponse(Guid Id, Guid RegisterId, string RegisterName, string Name, DateTime CreatedAt);
public sealed record ScannerPairingResponse(ScannerDeviceResponse Device, string WebhookUrl);
public sealed record ScannerReceiverResponse(Guid ReceiverId, Guid ShiftId, Guid RegisterId);
public sealed record ScannerScanEvent(string Barcode);

public sealed class CashierScannerApiClient(HttpClient http, SessionService session) : BaseApiClient(http, session)
{
    private const string Base = "api/v1/cashier/scanner";

    public Task<List<ScannerDeviceResponse>?> GetDevicesAsync(CancellationToken ct = default) =>
        GetAsync<List<ScannerDeviceResponse>>($"{Base}/devices", ct);

    public Task<ScannerPairingResponse?> CreateDeviceAsync(Guid registerId, string name, CancellationToken ct = default) =>
        PostAsync<ScannerPairingResponse>($"{Base}/devices", new { registerId, name }, ct);

    public Task<ScannerPairingResponse?> RotateDeviceAsync(Guid deviceId, CancellationToken ct = default) =>
        PostAsync<ScannerPairingResponse>($"{Base}/devices/{deviceId}/rotate", new { }, ct);

    public Task RevokeDeviceAsync(Guid deviceId, CancellationToken ct = default) =>
        DeleteHttpAsync($"{Base}/devices/{deviceId}", ct);

    public Task<ScannerReceiverResponse?> StartReceiverAsync(Guid shiftId, CancellationToken ct = default) =>
        PostAsync<ScannerReceiverResponse>($"{Base}/receivers", new { shiftId }, ct);

    public Task<List<ScannerScanEvent>?> PollAsync(Guid receiverId, Guid shiftId, bool paused = false, CancellationToken ct = default) =>
        GetAsync<List<ScannerScanEvent>>($"{Base}/receivers/{receiverId}/scans?shiftId={shiftId}&paused={paused}", ct);

    public Task StopReceiverAsync(Guid receiverId, Guid shiftId, CancellationToken ct = default) =>
        DeleteHttpAsync($"{Base}/receivers/{receiverId}?shiftId={shiftId}", ct);
}
