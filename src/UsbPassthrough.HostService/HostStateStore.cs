using System.Collections.Concurrent;
using UsbPassthrough.Common;

namespace UsbPassthrough.HostService;

public sealed class HostStateStore
{
    private readonly ConcurrentDictionary<string, AttachmentDto> _attachments = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentQueue<LogEntryDto> _logs = new();

    public void AddAttachment(AttachmentDto dto) => _attachments[dto.DeviceId] = dto;
    public bool RemoveAttachment(string deviceId) => _attachments.TryRemove(deviceId, out _);
    public IReadOnlyList<AttachmentDto> GetAttachments() => _attachments.Values.OrderByDescending(a => a.Since).ToList();

    public void Log(string level, string message)
    {
        _logs.Enqueue(new LogEntryDto(DateTimeOffset.UtcNow, level, message));
        while (_logs.Count > 500)
        {
            _ = _logs.TryDequeue(out _);
        }
    }

    public IReadOnlyList<LogEntryDto> RecentLogs(int max)
    {
        var normalized = Math.Clamp(max, 1, 200);
        return _logs.Reverse().Take(normalized).ToList();
    }
}