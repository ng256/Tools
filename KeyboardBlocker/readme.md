# Keyboard Blocker

Copyright © 2025 Pavel Bashkardin

## Description

Keyboard Blocker is a small Windows utility that completely blocks keyboard input. It runs in the background with an icon in the system tray (near the clock).

- **First launch:** keyboard is blocked, a notification balloon appears.
- **Second launch** (or selecting "Exit" from the tray menu): keyboard is unblocked, the application exits.

The program uses a low‑level keyboard hook (`WH_KEYBOARD_LL`) to intercept and discard all keyboard messages. It is intended for situations where you want to prevent a child (or anyone) from accidentally pressing keys while watching a video.

## Compilation Requirements

- Windows operating system.
- **MinGW‑w64** compiler (TDM‑GCC recommended) with `windres` (resource compiler) and `objdump` (usually included).
- **Optional:** [UPX](https://upx.github.io/) (Ultimate Packer for eXecutables) for further size reduction.

### Download Links

- [TDM-GCC (MinGW-w64)](https://jmeubank.github.io/tdm-gcc/)
- [Official MinGW-w64](https://www.mingw-w64.org/)
- [UPX](https://upx.github.io/) or [UPX on SourceForge](https://upx.sourceforge.net/)

The provided build script assumes the compiler is installed at:  
`C:\Program Files (x86)\Embarcadero\Dev-Cpp\TDM-GCC-64\bin`  
If your compiler is located elsewhere, edit the `set PATH=` line in `build.bat`.

## How to Build

Simply double‑click `build.bat` or run it from the command prompt.  
The script will:

1. Delete old object files and the previous executable.
2. Compile the resource script (`resource.rc`) into `resource.o`.
3. Compile the main source (`keyblock.c`) into `keyblock.o`.
4. Link everything into `keyblock.exe` with optimizations for small size.
5. Display the list of imported DLLs (to verify no unexpected dependencies).
6. If UPX is found, compress the executable (optional).

After a successful build you will find `keyblock.exe` in the same folder.

## Usage

- Run `keyblock.exe` once – the keyboard becomes blocked.
- Run it again – the first instance exits, unblocking the keyboard.
- Right‑click the tray icon and select **"Exit"** to unblock manually.
- Left‑click the tray icon to repeat the notification message.

The program is designed to be as unobtrusive as possible: no windows, only a tray icon and occasional balloons.

## License

This project is licensed under the MIT License – see the license in the source files for details. In short, you may use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the software, provided that the copyright notice and permission notice appear in all copies.

## Author

**Pavel Bashkardin**

For questions or suggestions, feel free to contact the author via GitHub or other channels (if applicable).