using System.Text.Json.Nodes;

namespace UsbPassthrough.Common;

public static class PipeNames
{
    public const string HostService = "UsbPassthrough.HostService";
}

public static class HostCommands
{
    public const string GetServiceStatus = nameof(GetServiceStatus);
    public const string ListUsbDevices = nameof(ListUsbDevices);
    public const string AttachDevice = nameof(AttachDevice);
    public const string DetachDevice = nameof(DetachDevice);
    public const string GetAttachments = nameof(GetAttachments);
    public const string GetRecentLogs = nameof(GetRecentLogs);
    public const string PatchConfig = nameof(PatchConfig);
    public const string GetDiagnostics = nameof(GetDiagnostics);
}

public sealed record HostRequest(string Command, JsonObject? Payload = null);
public sealed record HostResponse(bool Success, string? Error = null, JsonObject? Payload = null);

public sealed record ServiceStatusDto(bool Running, string Message, DateTimeOffset UtcNow);
public sealed record UsbDeviceDto(string DeviceId, string FriendlyName, string VidPid, string? Serial, string Status, string? AttachedVm);
public sealed record AttachmentDto(string DeviceId, string FriendlyName, string? ClientAddress, DateTimeOffset Since, string Status);
public sealed record LogEntryDto(DateTimeOffset Timestamp, string Level, string Message);

public sealed record AttachDeviceRequest(string DeviceId, string? ClientAddress);
public sealed record DetachDeviceRequest(string DeviceId);
public sealed record RecentLogsRequest(int Max);
public sealed record PatchConfigRequest(bool? StartWithWindows, bool? StartMinimizedToTray, bool? AutoReconnect, bool? Notifications, string? Psk, string? CertThumbprint);

public sealed record AppConfig
{
    public bool StartWithWindows { get; init; }
    public bool StartMinimizedToTray { get; init; }
    public bool AutoReconnect { get; init; } = true;
    public bool Notifications { get; init; } = true;
    public string? Psk { get; init; }
    public string? CertThumbprint { get; init; }
}

public sealed record GuestConfig
{
    public string HostAddress { get; init; } = "127.0.0.1";
    public int ControlPort { get; init; } = 3240;
    public bool AutoReconnect { get; init; } = true;
}
