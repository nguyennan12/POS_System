using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using POS.Api.Auth;
using POS.Application.Abstractions.Persistence;
using POS.Domain.Employees.Enums;
using POS.Domain.Rbac.Constants;
using POS.Domain.Stores;
using POS.Infrastructure.Persistence;
using StackExchange.Redis;

namespace POS.Api.Controllers;

/// <summary>Persistent phone pairing per register, with a short-lived cashier receiver.</summary>
[ApiController]
[Route("api/v1/cashier/scanner")]
public sealed class CashierScannerController(
    AppDbContext context,
    IConnectionMultiplexer redis,
    IShiftRepository shifts,
    CurrentUser currentUser,
    IConfiguration configuration,
    IHostEnvironment environment) : ControllerBase
{
    private static readonly TimeSpan ReceiverLifetime = TimeSpan.FromSeconds(15);
    private static readonly TimeSpan QueueLifetime = TimeSpan.FromHours(8);
    private static readonly string[] BarcodeFields =
        ["barcode", "barcodeData", "code", "value", "text", "data", "content", "scanResult"];
    private readonly IDatabase _db = redis.GetDatabase();

    public sealed record CreateDeviceRequest(Guid RegisterId, string Name);
    public sealed record DeviceResponse(Guid Id, Guid RegisterId, string RegisterName, string Name, DateTime CreatedAt);
    public sealed record PairingResponse(DeviceResponse Device, string WebhookUrl);
    public sealed record StartReceiverRequest(Guid ShiftId);
    public sealed record ReceiverResponse(Guid ReceiverId, Guid ShiftId, Guid RegisterId);
    public sealed record ScanEvent(string Barcode);

    [Authorize]
    [HttpGet("devices")]
    public async Task<ActionResult<IReadOnlyList<DeviceResponse>>> ListDevices(CancellationToken ct)
    {
        if (!await CanManageDevicesAsync(ct) || currentUser.StoreId is not Guid storeId) return Forbid();
        var devices = await (
            from d in context.ScannerDevices.AsNoTracking()
            join r in context.PosRegisters.AsNoTracking() on d.RegisterId equals r.Id
            where d.StoreId == storeId && d.IsActive
            orderby r.Code, d.Name
            select new DeviceResponse(d.Id, d.RegisterId, r.Name, d.Name, d.CreatedAt)
        ).ToListAsync(ct);
        return Ok(devices);
    }

    [Authorize]
    [HttpPost("devices")]
    public async Task<ActionResult<PairingResponse>> CreateDevice(
        [FromBody] CreateDeviceRequest request, CancellationToken ct)
    {
        if (!await CanManageDevicesAsync(ct) || currentUser.StoreId is not Guid storeId) return Forbid();
        var register = await context.PosRegisters.AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == request.RegisterId && r.StoreId == storeId && r.IsActive, ct);
        if (register is null) return BadRequest(new { message = "Quầy không hợp lệ hoặc đã ngừng hoạt động." });
        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Trim().Length > 100)
            return BadRequest(new { message = "Tên thiết bị phải từ 1 đến 100 ký tự." });
        if (!TryGetPublicBaseUri(out var baseUri)) return ScannerUrlProblem();

        var secret = NewSecret();
        var device = new ScannerDevice(storeId, register.Id, request.Name, Hash(secret));
        context.ScannerDevices.Add(device);
        await context.SaveChangesAsync(ct);
        return Ok(new PairingResponse(
            new DeviceResponse(device.Id, register.Id, register.Name, device.Name, device.CreatedAt),
            WebhookUrl(baseUri!, device.Id, secret)));
    }

    [Authorize]
    [HttpPost("devices/{id:guid}/rotate")]
    public async Task<ActionResult<PairingResponse>> RotateDevice(Guid id, CancellationToken ct)
    {
        if (!await CanManageDevicesAsync(ct) || currentUser.StoreId is not Guid storeId) return Forbid();
        var device = await context.ScannerDevices
            .FirstOrDefaultAsync(d => d.Id == id && d.StoreId == storeId && d.IsActive, ct);
        if (device is null) return NotFound();
        if (!TryGetPublicBaseUri(out var baseUri)) return ScannerUrlProblem();
        var registerName = await context.PosRegisters.AsNoTracking()
            .Where(r => r.Id == device.RegisterId).Select(r => r.Name).SingleAsync(ct);

        var secret = NewSecret();
        device.Rotate(Hash(secret));
        await context.SaveChangesAsync(ct);
        return Ok(new PairingResponse(
            new DeviceResponse(device.Id, device.RegisterId, registerName, device.Name, device.CreatedAt),
            WebhookUrl(baseUri!, device.Id, secret)));
    }

    [Authorize]
    [HttpDelete("devices/{id:guid}")]
    public async Task<IActionResult> RevokeDevice(Guid id, CancellationToken ct)
    {
        if (!await CanManageDevicesAsync(ct) || currentUser.StoreId is not Guid storeId) return Forbid();
        var device = await context.ScannerDevices
            .FirstOrDefaultAsync(d => d.Id == id && d.StoreId == storeId && d.IsActive, ct);
        if (device is null) return NoContent();
        device.Revoke();
        await context.SaveChangesAsync(ct);
        return NoContent();
    }

    [Authorize]
    [HttpPost("receivers")]
    public async Task<ActionResult<ReceiverResponse>> StartReceiver(
        [FromBody] StartReceiverRequest request, CancellationToken ct)
    {
        var shift = await shifts.GetByIdAsync(request.ShiftId, ct);
        if (shift is null || shift.Status != ShiftStatus.Open ||
            shift.EmployeeId != currentUser.EmployeeId || shift.StoreId != currentUser.StoreId)
            return Forbid();
        var receiverId = Guid.NewGuid();
        await _db.StringSetAsync(
            ReceiverKey(shift.RegisterId),
            ReceiverValue(shift.Id, shift.EmployeeId, receiverId),
            ReceiverLifetime);
        return Ok(new ReceiverResponse(receiverId, shift.Id, shift.RegisterId));
    }

    [Authorize]
    [HttpGet("receivers/{receiverId:guid}/scans")]
    public async Task<ActionResult<IReadOnlyList<ScanEvent>>> Poll(
        Guid receiverId, [FromQuery] Guid shiftId, [FromQuery] bool paused, CancellationToken ct)
    {
        var shift = await shifts.GetByIdAsync(shiftId, ct);
        if (shift is null || shift.Status != ShiftStatus.Open ||
            shift.EmployeeId != currentUser.EmployeeId || shift.StoreId != currentUser.StoreId)
            return Forbid();
        var key = ReceiverKey(shift.RegisterId);
        var expected = ReceiverValue(shift.Id, shift.EmployeeId, receiverId);
        if (await _db.StringGetAsync(key) != expected)
            return Conflict(new { message = "Quầy đang được một màn Cashier khác nhận mã hoặc kết nối đã hết hạn." });
        await _db.KeyExpireAsync(key, ReceiverLifetime);
        if (paused) return Ok(Array.Empty<ScanEvent>());

        var scans = new List<ScanEvent>();
        for (var i = 0; i < 20; i++)
        {
            var value = await _db.ListLeftPopAsync(QueueKey(shift.Id));
            if (value.IsNullOrEmpty) break;
            scans.Add(new ScanEvent(value.ToString()));
        }
        return Ok(scans);
    }

    [Authorize]
    [HttpDelete("receivers/{receiverId:guid}")]
    public async Task<IActionResult> StopReceiver(
        Guid receiverId, [FromQuery] Guid shiftId, CancellationToken ct)
    {
        var shift = await shifts.GetByIdAsync(shiftId, ct);
        if (shift is null || shift.EmployeeId != currentUser.EmployeeId ||
            shift.StoreId != currentUser.StoreId) return Forbid();
        var key = ReceiverKey(shift.RegisterId);
        if (await _db.StringGetAsync(key) == ReceiverValue(shift.Id, shift.EmployeeId, receiverId))
            await _db.KeyDeleteAsync(key);
        return NoContent();
    }

    [AllowAnonymous]
    [HttpPost("webhook/{id:guid}")]
    [RequestSizeLimit(16 * 1024)]
    public async Task<IActionResult> Receive(Guid id, [FromQuery] string? key, CancellationToken ct)
    {
        var device = await context.ScannerDevices.AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == id && d.IsActive, ct);
        if (device is null || string.IsNullOrWhiteSpace(key) ||
            !CryptographicOperations.FixedTimeEquals(
                Convert.FromHexString(device.SecretHash), Convert.FromHexString(Hash(key))))
            return Unauthorized();

        var shift = await shifts.GetOpenShiftByRegisterAsync(device.RegisterId, ct);
        if (shift is null || shift.StoreId != device.StoreId)
            return Conflict(new { message = "Quầy chưa mở ca bán." });
        var receiver = await _db.StringGetAsync(ReceiverKey(device.RegisterId));
        if (receiver.IsNullOrEmpty || !receiver.ToString().StartsWith($"{shift.Id:N}:{shift.EmployeeId:N}:", StringComparison.Ordinal))
            return Conflict(new { message = "Màn Cashier của quầy chưa sẵn sàng nhận mã." });

        var barcode = await ReadBarcodeAsync(ct);
        if (string.IsNullOrWhiteSpace(barcode) || barcode.Length > 128)
            return BadRequest(new { message = "Không tìm thấy barcode hợp lệ trong dữ liệu quét." });

        await _db.ListRightPushAsync(QueueKey(shift.Id), barcode);
        await _db.KeyExpireAsync(QueueKey(shift.Id), QueueLifetime);
        return Accepted(new { received = true, barcode });
    }

    private async Task<bool> CanManageDevicesAsync(CancellationToken ct) =>
        currentUser.IsChainOwner ||
        (currentUser.RoleId is Guid roleId &&
         await context.Roles.AsNoTracking().AnyAsync(
             role => role.Id == roleId &&
                     (role.Name == RoleNames.Owner || role.Name == RoleNames.StoreManager), ct));

    private bool TryGetPublicBaseUri(out Uri? uri)
    {
        var value = configuration["Scanner:PublicBaseUrl"];
        return Uri.TryCreate(value, UriKind.Absolute, out uri) &&
               uri.Scheme is "http" or "https" &&
               (environment.IsDevelopment() || uri.Scheme == "https");
    }

    private ObjectResult ScannerUrlProblem() =>
        Problem("Scanner:PublicBaseUrl phải là địa chỉ truy cập được từ điện thoại; production yêu cầu HTTPS.", statusCode: 503);

    private static string NewSecret() => Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    private static string WebhookUrl(Uri baseUri, Guid id, string secret) =>
        new Uri(baseUri, $"/api/v1/cashier/scanner/webhook/{id}?key={secret}").ToString();
    private static string ReceiverKey(Guid registerId) => $"pos:cashier:scanner:receiver:{registerId:N}";
    private static string QueueKey(Guid shiftId) => $"pos:cashier:scanner:queue:{shiftId:N}";
    private static string ReceiverValue(Guid shiftId, Guid employeeId, Guid receiverId) =>
        $"{shiftId:N}:{employeeId:N}:{receiverId:N}";

    private async Task<string?> ReadBarcodeAsync(CancellationToken ct)
    {
        if (Request.HasFormContentType)
        {
            var form = await Request.ReadFormAsync(ct);
            foreach (var name in BarcodeFields)
                if (form.TryGetValue(name, out var value) && !string.IsNullOrWhiteSpace(value))
                    return value.ToString().Trim();
            return null;
        }
        using var reader = new StreamReader(Request.Body, Encoding.UTF8);
        var body = (await reader.ReadToEndAsync(ct)).Trim();
        if (body.Length == 0) return null;
        try
        {
            using var json = JsonDocument.Parse(body);
            if (json.RootElement.ValueKind == JsonValueKind.String)
                return json.RootElement.GetString()?.Trim();
            if (json.RootElement.ValueKind == JsonValueKind.Object)
                foreach (var name in BarcodeFields)
                {
                    var field = json.RootElement.EnumerateObject()
                        .FirstOrDefault(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
                    if (field.Value.ValueKind == JsonValueKind.String)
                        return field.Value.GetString()?.Trim();
                }
        }
        catch (JsonException) { }
        if (body.Contains('='))
            foreach (var pair in body.Split('&', StringSplitOptions.RemoveEmptyEntries))
            {
                var parts = pair.Split('=', 2);
                if (parts.Length == 2 && BarcodeFields.Contains(Uri.UnescapeDataString(parts[0]), StringComparer.OrdinalIgnoreCase))
                    return Uri.UnescapeDataString(parts[1].Replace('+', ' ')).Trim();
            }
        return body.Length <= 128 && !body.StartsWith('{') ? body : null;
    }
}
