# TROUBLESHOOTING

## Häufige Fehler

- driver not installed: Host-Treiberstatus prüfen (`usbipd state`).
- bind failed: Adminrechte, Device exklusiv belegt oder Policy blockiert.
- device busy: Gerät ist bereits lokal/remote in Nutzung.
- attach timeout: Netzwerk/Firewall oder Guest-Client nicht erreichbar.
- access denied: Service/CLI ohne erhöhte Rechte.

## PowerShell Quick Checks

- `Get-Service usbipd,UsbPassthroughHost,UsbPassthroughGuest`
- `usbipd list`
- `usbipd state`
- `Get-WinEvent -LogName Application -MaxEvents 100 | ? Message -match 'UsbPassthrough|usbipd'`