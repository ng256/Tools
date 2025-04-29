# Ping All

This script, __pingall.bat__, allows you to check the availability of hosts or IP addresses using the ping command. It provides a simple text-based list input and uses ANSI color codes for better readability.

## 📄 Description

The script reads lines from a file (default: ip.txt in the same folder), interpreting each line as:

```
; Comment
<IP> <description> [optional note]
```

Lines starting with ; are ignored.

If only one word is provided, it is considered a title.

Colored output enhances visibility:

🟢 Green: host is online.

🔴 Red: host is offline.

🟡 Yellow: descriptive text.

## 🧠 How It Works

Written in pure cmd.exe batch language, this script utilizes ANSI escape codes for colorized terminal output.

Key Functions

:setEsc — extracts the ESC character using a trick with prompt.

:iter — parses each line: either a title or an IP with a label.

:doPing — performs a quick ping and prints the result with a status color.

:title — prints a highlighted section title.

📦 Input File Format (ip.txt)

Example:
```
; this is a comment
SERVERS

192.168.1.1 Router
192.168.1.100 NAS
192.168.1.200 DB Server

CAMERAS

192.168.1.201 Camera #1
192.168.1.202 Camera #2
```
## 📌 Running the Script
```
pingall.bat ip.txt
```
If no argument is given, ip.txt is used from the script's folder.

🖌 ANSI Colors

- ESC[33m — yellow (description)

- ESC[92m — green (online)

- ESC[91m — red (offline)

- ESC[4;41;93m — section title (underlined, red background)

## 🛠 Compatibility

Windows 10 and newer (ANSI escape codes enabled by default)

Older versions may require enabling ANSI via registry or third-party tools.

## ⚠️ Limitations

No parallel pings — checks are performed sequentially.

No logging or advanced diagnostics.

## 📚 License

MIT License
