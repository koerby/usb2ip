using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using UsbPassthrough.Common;

namespace UsbPassthrough.Backend;

public sealed class UsbBackend(string toolPath = "usbipd") : IUsbBackend
{
    public Task<IReadOnlyList<UsbDeviceDto>> ListDevices(CancellationToken cancellationToken) =>
        RunAndParseList(cancellationToken);

    public Task<BackendResult> BindDevice(string deviceId, CancellationToken cancellationToken) =>
        Run("bind", $"--busid={deviceId}", cancellationToken);

    public Task<BackendResult> UnbindDevice(string deviceId, CancellationToken cancellationToken) =>
        Run("unbind", $"--busid={deviceId}", cancellationToken);

    public Task<BackendResult> ExportDevice(string deviceId, CancellationToken cancellationToken) =>
        Run("bind", $"--busid={deviceId}", cancellationToken);

    public Task<BackendResult> AttachOnGuest(string hostAddress, string deviceId, CancellationToken cancellationToken) =>
        Run("attach", $"--remote={hostAddress}", $"--busid={deviceId}", cancellationToken);

    public Task<BackendResult> DetachOnGuest(string deviceId, CancellationToken cancellationToken) =>
        Run("detach", $"--busid={deviceId}", cancellationToken);

    public async Task<DriverStatusResult> GetDriverStatus(CancellationToken cancellationToken)
    {
        var result = await Run("state", cancellationToken: cancellationToken).ConfigureAwait(false);
        if (!result.Success)
        {
            return new DriverStatusResult(false, result.Message ?? "Driver status unavailable", result.ErrorCode);
        }
        return new DriverStatusResult(true, "usbipd reachable");
    }

    async Task<IReadOnlyList<UsbDeviceDto>> RunAndParseList(CancellationToken cancellationToken)
    {
        var exec = await Execute(cancellationToken, "list").ConfigureAwait(false);
        if (!exec.Success)
        {
            throw new InvalidOperationException(exec.Message ?? "usbipd list failed");
        }

        return UsbipdListParser.Parse(exec.StdOut);
    }

    Task<BackendResult> Run(string command, string? arg = null, CancellationToken cancellationToken = default)
    {
        var args = arg is null ? [command] : new[] { command, arg };
        return Run(args, cancellationToken);
    }

    Task<BackendResult> Run(string command, string arg1, string arg2, CancellationToken cancellationToken)
        => Run([command, arg1, arg2], cancellationToken);

    async Task<BackendResult> Run(string[] args, CancellationToken cancellationToken)
    {
        var exec = await Execute(cancellationToken, args).ConfigureAwait(false);
        if (exec.Success)
        {
            return new BackendResult(true);
        }
        return new BackendResult(false, exec.ErrorCode, exec.Message);
    }

    async Task<(bool Success, string StdOut, BackendErrorCode ErrorCode, string Message)> Execute(CancellationToken cancellationToken, params string[] args)
    {
        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = toolPath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        foreach (var arg in args)
        {
            process.StartInfo.ArgumentList.Add(arg);
        }

        var output = new StringBuilder();
        var error = new StringBuilder();
        process.OutputDataReceived += (_, e) => { if (e.Data is not null) { _ = output.AppendLine(e.Data); } };
        process.ErrorDataReceived += (_, e) => { if (e.Data is not null) { _ = error.AppendLine(e.Data); } };

        try
        {
            if (!process.Start())
            {
                return (false, string.Empty, BackendErrorCode.ProcessFailed, "Could not start usbipd process");
            }
        }
        catch (Exception ex) when (ex is Win32Exception or FileNotFoundException)
        {
            return (false, string.Empty, BackendErrorCode.ToolNotFound, ex.Message);
        }

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(20));

        try
        {
            await process.WaitForExitAsync(timeoutCts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            try { process.Kill(true); }
            catch (InvalidOperationException) { }
            return (false, output.ToString(), BackendErrorCode.Timeout, "usbipd command timed out");
        }

        var stdout = output.ToString();
        var stderr = error.ToString().Trim();
        if (process.ExitCode == 0)
        {
            return (true, stdout, BackendErrorCode.None, string.Empty);
        }

        var mapped = process.ExitCode switch
        {
            5 => BackendErrorCode.Unauthorized,
            _ => BackendErrorCode.ProcessFailed,
        };
        var message = string.IsNullOrWhiteSpace(stderr) ? $"usbipd failed with exit code {process.ExitCode}" : stderr;
        return (false, stdout, mapped, message);
    }
}