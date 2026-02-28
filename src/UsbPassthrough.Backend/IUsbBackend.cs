using UsbPassthrough.Common;

namespace UsbPassthrough.Backend;

public interface IUsbBackend
{
    Task<IReadOnlyList<UsbDeviceDto>> ListDevices(CancellationToken cancellationToken);
    Task<BackendResult> BindDevice(string deviceId, CancellationToken cancellationToken);
    Task<BackendResult> UnbindDevice(string deviceId, CancellationToken cancellationToken);
    Task<BackendResult> ExportDevice(string deviceId, CancellationToken cancellationToken);
    Task<BackendResult> AttachOnGuest(string hostAddress, string deviceId, CancellationToken cancellationToken);
    Task<BackendResult> DetachOnGuest(string deviceId, CancellationToken cancellationToken);
    Task<DriverStatusResult> GetDriverStatus(CancellationToken cancellationToken);
}

public enum BackendErrorCode
{
    None,
    Timeout,
    ProcessFailed,
    Unauthorized,
    ToolNotFound,
    ParseError,
    Unknown
}

public sealed record BackendResult(bool Success, BackendErrorCode ErrorCode = BackendErrorCode.None, string? Message = null);
public sealed record DriverStatusResult(bool Installed, string Message, BackendErrorCode ErrorCode = BackendErrorCode.None);