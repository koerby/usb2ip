using UsbPassthrough.Backend;

var backend = new UsbBackend();

if (args.Length == 0 || args[0].Equals("help", StringComparison.OrdinalIgnoreCase) || args[0].Equals("--help", StringComparison.OrdinalIgnoreCase))
{
    PrintHelp();
    return;
}

switch (args[0].ToLowerInvariant())
{
    case "list":
    {
        var devices = await backend.ListDevices(CancellationToken.None).ConfigureAwait(false);
        foreach (var device in devices)
        {
            Console.WriteLine($"{device.DeviceId} {device.VidPid} {device.FriendlyName} [{device.Status}]");
        }
        break;
    }
    case "connect":
    {
        var remote = GetOption(args, "--remote") ?? GetOption(args, "-r");
        var busId = GetOption(args, "--busid") ?? GetOption(args, "-b");
        if (string.IsNullOrWhiteSpace(remote) || string.IsNullOrWhiteSpace(busId))
        {
            Console.Error.WriteLine("Fehler: connect benötigt --remote und --busid.");
            PrintHelp();
            Environment.ExitCode = 2;
            return;
        }

        var result = await backend.AttachOnGuest(remote, busId, CancellationToken.None).ConfigureAwait(false);
        Console.WriteLine(result.Success ? $"Connected {busId} @ {remote}" : $"Connect fehlgeschlagen: {result.ErrorCode} {result.Message}");
        Environment.ExitCode = result.Success ? 0 : 1;
        break;
    }
    case "disconnect":
    {
        var busId = GetOption(args, "--busid") ?? GetOption(args, "-b");
        if (string.IsNullOrWhiteSpace(busId))
        {
            Console.Error.WriteLine("Fehler: disconnect benötigt --busid.");
            PrintHelp();
            Environment.ExitCode = 2;
            return;
        }

        var result = await backend.DetachOnGuest(busId, CancellationToken.None).ConfigureAwait(false);
        Console.WriteLine(result.Success ? $"Disconnected {busId}" : $"Disconnect fehlgeschlagen: {result.ErrorCode} {result.Message}");
        Environment.ExitCode = result.Success ? 0 : 1;
        break;
    }
    default:
        Console.Error.WriteLine($"Unbekannter Befehl: {args[0]}");
        PrintHelp();
        Environment.ExitCode = 2;
        break;
}

static string? GetOption(string[] args, string option)
{
    for (var i = 0; i < args.Length - 1; i++)
    {
        if (args[i].Equals(option, StringComparison.OrdinalIgnoreCase))
        {
            return args[i + 1];
        }
    }
    return null;
}

static void PrintHelp()
{
    Console.WriteLine("usb-guest-client");
    Console.WriteLine("  list");
    Console.WriteLine("  connect --remote <HOST/IP> --busid <BUSID>");
    Console.WriteLine("  disconnect --busid <BUSID>");
}