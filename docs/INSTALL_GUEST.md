# INSTALL_GUEST

1. `usb-guest-agent.exe` deployen (oder `install-guest-agent.bat` aus `dist\\guest`).
2. `%ProgramData%\\UsbPassthrough\\guest.json` prüfen/anpassen.
3. Service registrieren: `sc create UsbPassthroughGuest binPath= "...\\usb-guest-agent.exe"`.
4. Client für Verbindungen nutzen: `client.exe connect --remote <HOST-IP> --busid <BUSID>`.
5. Bei Bedarf clientseitige VHCI/USBIP-Komponenten installieren.