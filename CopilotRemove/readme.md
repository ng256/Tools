# Copilot Removal Utility
A lightweight Windows utility that removes Microsoft Copilot components, disables related policies, and hides the Copilot button from the taskbar. The program is written in pure WinAPI with no C Runtime Library (CRT) dependencies, resulting in a small standalone executable.

## Features
- Removes Copilot AppxPackages for all users and from the system image.
- Disables Copilot via Group Policies in both HKEY_CURRENT_USER and HKEY_LOCAL_MACHINE.
- Hides the Copilot taskbar button by setting the appropriate registry value.
- Administrator privilege check – ensures the tool is run with elevated rights.
- Waits for a key press before exiting (so you can read the results even if launched from Explorer).
- No CRT dependencies – uses only Windows API functions.

## Requirements
- Windows 10 or Windows 11 (may also work on older versions but Copilot is not present there).
- Administrator privileges (the program checks and exits with an error if not elevated).

## Usage
Download the compiled copilot_remove.exe (or compile it yourself – see below).

Right‑click the executable and select “Run as administrator”.

Follow the on‑screen messages. The utility will:

1. Remove all Copilot AppxPackages.
2. Set the TurnOffWindowsCopilot policy to 1 in both user and machine hives.
3. Set ShowCopilotButton to 0 in Explorer settings.

After completion, press any key to close the window.

Reboot your computer to ensure all changes take effect.

## Compilation
You can compile the program using MinGW-w64 (GCC). No additional libraries are needed besides the standard Windows ones.

```cmd
gcc -o copilot_remove.exe copilot_remove.c -ladvapi32
```
If you prefer a console-less version (no window when double‑clicked), add the -mwindows flag:

```cmd
gcc -mwindows -o copilot_remove.exe copilot_remove.c -ladvapi32
```
Note: The -mwindows version still prints to the console if launched from a command prompt, but it won't open a new console window when started from Explorer. The “press any key” prompt may not work properly in that mode because stdin might not be available.

## How It Works
Registry tweaks: Uses RegCreateKeyExA and RegSetValueExA to write DWORD values.

Package removal: Spawns hidden PowerShell processes with CreateProcessA to run Remove-AppxPackage and Remove-AppxProvisionedPackage.

## Disclaimer
This utility modifies system settings and removes built‑in Windows components. Use it at your own risk. It is recommended to create a system restore point before running. The author is not responsible for any damage or data loss.

## License
This project is provided under the MIT License. Feel free to modify and distribute it as you wish.

## Author
**Pavel Bashkardin**
