# Target Architecture

- `UsbPassthrough.Backend` ist der einzige Layer mit direktem Zugriff auf `usbipd`-Tooling / Driver Calls.
- `UsbPassthrough.HostService` ist der Host/Server-Dienst für Share/Unshare und Status.
- `UsbPassthrough.HostTray` (WPF, Light-Mode) steuert den Host ausschließlich über Named-Pipe-IPC.
- `UsbPassthrough.GuestAgent` ist der Guest/Client-Dienst für Reconnect-Logik.
- `UsbPassthrough.Cli` liefert `usb-guest-client.exe` für Connect/Disconnect vom Guest zum Host über IP.