using System.Collections.ObjectModel;
using System.IO.Compression;
using System.IO;
using System.ServiceProcess;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using Hardcodet.Wpf.TaskbarNotification;
using ModernWpf;
using UsbPassthrough.Common;

namespace UsbPassthrough.HostTray;

public partial class MainWindow : Window
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly HostServiceClient _client = new();
    private readonly TaskbarIcon _trayIcon;

    private readonly ObservableCollection<UsbDeviceDto> _devices = [];
    private readonly ObservableCollection<AttachmentDto> _attachments = [];
    private readonly ObservableCollection<LogEntryDto> _logs = [];

    public MainWindow()
    {
        ThemeManager.Current.ApplicationTheme = ApplicationTheme.Light;

        InitializeComponent();
        DevicesGrid.ItemsSource = _devices;
        AttachmentGrid.ItemsSource = _attachments;
        LogsGrid.ItemsSource = _logs;

        _trayIcon = BuildTrayIcon();

        Loaded += async (_, _) => await RefreshAsync();
        Closed += (_, _) => _trayIcon.Dispose();
    }

    private TaskbarIcon BuildTrayIcon()
    {
        var menu = new System.Windows.Controls.ContextMenu();
        menu.Items.Add(CreateMenu("Open", (_, _) => ShowAndActivate()));
        menu.Items.Add(CreateMenu("Start Service", async (_, _) => await ControlHostService("start")));
        menu.Items.Add(CreateMenu("Stop Service", async (_, _) => await ControlHostService("stop")));
        menu.Items.Add(CreateMenu("Restart Service", async (_, _) => await ControlHostService("restart")));
        menu.Items.Add(new Separator());
        menu.Items.Add(CreateMenu("Exit", (_, _) => System.Windows.Application.Current.Shutdown()));

        var iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "Icons", "host-tray.ico");
        var icon = File.Exists(iconPath)
            ? new System.Drawing.Icon(iconPath)
            : System.Drawing.SystemIcons.Application;

        return new TaskbarIcon
        {
            Icon = icon,
            ToolTipText = "USB Passthrough Host",
            ContextMenu = menu,
        };
    }

    private static MenuItem CreateMenu(string header, RoutedEventHandler action)
    {
        var item = new MenuItem { Header = header };
        item.Click += action;
        return item;
    }

    private async Task ControlHostService(string action)
    {
        try
        {
            using var sc = new ServiceController("UsbPassthroughHost");
            switch (action)
            {
                case "start":
                    if (sc.Status is ServiceControllerStatus.Stopped or ServiceControllerStatus.StopPending)
                    {
                        sc.Start();
                        sc.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(10));
                    }
                    break;
                case "stop":
                    if (sc.Status is ServiceControllerStatus.Running or ServiceControllerStatus.StartPending)
                    {
                        sc.Stop();
                        sc.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(10));
                    }
                    break;
                case "restart":
                    if (sc.Status is ServiceControllerStatus.Running or ServiceControllerStatus.StartPending)
                    {
                        sc.Stop();
                        sc.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(10));
                    }
                    sc.Start();
                    sc.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(10));
                    break;
            }

            _trayIcon.ShowBalloonTip("USB Passthrough", $"Service {action} erfolgreich", BalloonIcon.Info);
        }
        catch (Exception ex)
        {
            _trayIcon.ShowBalloonTip("USB Passthrough", $"Service {action} fehlgeschlagen: {ex.Message}", BalloonIcon.Error);
        }

        await RefreshServiceStatus();
    }

    private async Task RefreshAsync()
    {
        await RefreshServiceStatus();
        await RefreshDevices();
        await RefreshAttachments();
        await RefreshLogs();
    }

    private async Task RefreshServiceStatus()
    {
        try
        {
            var response = await _client.SendAsync(HostCommands.GetServiceStatus);
            ServiceStatusText.Text = response.Success ? "Service: Running" : "Service: Stopped";
        }
        catch
        {
            ServiceStatusText.Text = "Service: Unreachable";
        }
    }

    private async Task RefreshDevices()
    {
        _devices.Clear();
        foreach (var item in await _client.ListAsync<UsbDeviceDto>(HostCommands.ListUsbDevices))
        {
            _devices.Add(item);
        }
    }

    private async Task RefreshAttachments()
    {
        _attachments.Clear();
        foreach (var item in await _client.ListAsync<AttachmentDto>(HostCommands.GetAttachments))
        {
            _attachments.Add(item);
        }
    }

    private async Task RefreshLogs()
    {
        _logs.Clear();
        var response = await _client.SendAsync(HostCommands.GetRecentLogs, new { max = 200 });
        var entries = response.Payload?["data"]?.Deserialize<List<LogEntryDto>>() ?? [];
        foreach (var log in entries)
        {
            _logs.Add(log);
        }
    }

    private void ShowAndActivate()
    {
        Show();
        WindowState = WindowState.Normal;
        Activate();
    }

    private void OnNavChanged(object sender, SelectionChangedEventArgs e)
    {
        MainTabs.SelectedIndex = NavList.SelectedIndex;
    }

    private async void OnShareDevice(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.Button button || button.Tag is not string deviceId)
        {
            return;
        }

        var response = await _client.SendAsync(HostCommands.AttachDevice, new AttachDeviceRequest(deviceId, null));
        _trayIcon.ShowBalloonTip("USB Passthrough", response.Success ? $"Gerät geteilt: {deviceId}" : $"Share fehlgeschlagen: {response.Error}", BalloonIcon.Info);
        await RefreshAsync();
    }

    private async void OnUnshareDevice(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.Button button || button.Tag is not string deviceId)
        {
            return;
        }

        var response = await _client.SendAsync(HostCommands.DetachDevice, new DetachDeviceRequest(deviceId));
        _trayIcon.ShowBalloonTip("USB Passthrough", response.Success ? $"Gerät freigegeben entfernt: {deviceId}" : $"Unshare fehlgeschlagen: {response.Error}", BalloonIcon.Info);
        await RefreshAsync();
    }

    private void OnSearchChanged(object sender, TextChangedEventArgs e)
    {
        var text = SearchBox.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(text))
        {
            DevicesGrid.Items.Filter = null;
            return;
        }

        DevicesGrid.Items.Filter = row =>
        {
            if (row is not UsbDeviceDto d)
            {
                return false;
            }
            return d.FriendlyName.Contains(text, StringComparison.OrdinalIgnoreCase)
                || d.VidPid.Contains(text, StringComparison.OrdinalIgnoreCase)
                || d.DeviceId.Contains(text, StringComparison.OrdinalIgnoreCase);
        };
    }

    private async void OnSaveSettings(object sender, RoutedEventArgs e)
    {
        var response = await _client.SendAsync(HostCommands.PatchConfig, new PatchConfigRequest(
            StartWithWindows.IsChecked,
            StartMinimized.IsChecked,
            AutoReconnect.IsChecked,
            Notifications.IsChecked,
            PskBox.Text,
            CertBox.Text));

        _trayIcon.ShowBalloonTip("USB Passthrough", response.Success ? "Settings saved" : $"Save failed: {response.Error}", BalloonIcon.Info);
    }

    private void OnExportLogs(object sender, RoutedEventArgs e)
    {
        var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), $"usb-passthrough-logs-{DateTime.Now:yyyyMMdd-HHmmss}.log");
        var builder = new StringBuilder();
        foreach (var log in _logs)
        {
            _ = builder.AppendLine($"{log.Timestamp:o} [{log.Level}] {log.Message}");
        }
        File.WriteAllText(path, builder.ToString());
        _trayIcon.ShowBalloonTip("USB Passthrough", $"Logs exportiert: {path}", BalloonIcon.Info);
    }

    private async void OnCreateDiagnostics(object sender, RoutedEventArgs e)
    {
        var response = await _client.SendAsync(HostCommands.GetDiagnostics);
        var root = Path.Combine(Path.GetTempPath(), "UsbPassthroughDiag", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        File.WriteAllText(Path.Combine(root, "diagnostics.json"), JsonSerializer.Serialize(response, JsonOptions));
        var bundle = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), $"usb-passthrough-diagnostics-{DateTime.Now:yyyyMMdd-HHmmss}.zip");
        ZipFile.CreateFromDirectory(root, bundle);
        Directory.Delete(root, true);

        _trayIcon.ShowBalloonTip("USB Passthrough", $"Diagnostics erstellt: {bundle}", BalloonIcon.Info);
    }
}