/*
 * Copilot Removal Utility - A lightweight Windows utility that removes Microsoft
 * Copilot components, disables related policies, and hides the Copilot button
 * from the taskbar.
 *
 * Copyright (c) 2025 Pavel Bashkardin
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 *
 * Description: The program is written in pure WinAPI with no C Runtime Library
 * dependencies, resulting in a small standalone executable.
 */

#define _CRT_SECURE_NO_WARNINGS   // for strsafe.h (optional)
#include "remove_copilot.h"

// STD streams global handles
HANDLE g_hStdOut = NULL;
HANDLE g_hStdErr = NULL;
HANDLE g_hStdIn  = NULL;

//------------------------------------------------------------------------
// Custom printing to a handle (supports %s and %lu)
//------------------------------------------------------------------------
void PrintFormat(HANDLE hOut, const char* fmt, va_list args)
{
    char buffer[2048];
    char* p = buffer;
    const char* f = fmt;

    while (*f && (p - buffer) < (int)sizeof(buffer) - 1)
    {
        if (*f != '%')
        {
            *p++ = *f++;
            continue;
        }

        f++; // skip '%'

        if (*f == 's')
        {
            const char* s = va_arg(args, const char*);
            if (!s) s = "(null)";
            while (*s && (p - buffer) < (int)sizeof(buffer) - 1)
                *p++ = *s++;
            f++;
        }
        else if (*f == 'l' && *(f + 1) == 'u')
        {
            unsigned long val = va_arg(args, unsigned long);
            char numbuf[32];
            int i = 0;

            if (val == 0)
            {
                numbuf[i++] = '0';
            }
            else
            {
                unsigned long n = val;
                while (n)
                {
                    numbuf[i++] = (char)('0' + (n % 10));
                    n /= 10;
                }
                // reverse
                for (int j = 0; j < i / 2; j++)
                {
                    char tmp = numbuf[j];
                    numbuf[j] = numbuf[i - 1 - j];
                    numbuf[i - 1 - j] = tmp;
                }
            }

            for (int j = 0; j < i && (p - buffer) < (int)sizeof(buffer) - 1; j++)
                *p++ = numbuf[j];

            f += 2; // skip "lu"
        }
        else
        {
            *p++ = '%';
            if (*f)
                *p++ = *f++;
        }
    }

    *p = '\0';
    DWORD written;
    WriteFile(hOut, buffer, (DWORD)(p - buffer), &written, NULL);
}

//------------------------------------------------------------------------
// Print with color (if handle is a console)
//------------------------------------------------------------------------
void PrintColored(HANDLE hOut, WORD color, const char* fmt, va_list args)
{
    if (hOut && hOut != INVALID_HANDLE_VALUE)
    {
        CONSOLE_SCREEN_BUFFER_INFO csbi;
        WORD oldAttr = 0;
        BOOL hasColor = FALSE;

        DWORD mode;
        if (GetConsoleMode(hOut, &mode))
        {
            if (GetConsoleScreenBufferInfo(hOut, &csbi))
            {
                oldAttr = csbi.wAttributes;
                SetConsoleTextAttribute(hOut, color);
                hasColor = TRUE;
            }
        }

        PrintFormat(hOut, fmt, args);

        if (hasColor)
        {
            SetConsoleTextAttribute(hOut, oldAttr);
        }
    }
}

//------------------------------------------------------------------------
// Print to stdout (no color)
//------------------------------------------------------------------------
void Print(const char* fmt, ...)
{
    if (g_hStdOut && g_hStdOut != INVALID_HANDLE_VALUE)
    {
        va_list args;
        va_start(args, fmt);
        va_list argsCopy;
        VA_COPY(argsCopy, args);
        PrintFormat(g_hStdOut, fmt, argsCopy);
        va_end(argsCopy);
        va_end(args);
    }
}

//------------------------------------------------------------------------
// Print warning message (yellow) to stdout
//------------------------------------------------------------------------
void PrintWarning(const char* fmt, ...)
{
    if (g_hStdOut && g_hStdOut != INVALID_HANDLE_VALUE)
    {
        va_list args;
        va_start(args, fmt);
        va_list argsCopy;
        VA_COPY(argsCopy, args);
        PrintColored(g_hStdOut, FOREGROUND_GREEN | FOREGROUND_RED | FOREGROUND_INTENSITY, fmt, argsCopy);
        va_end(argsCopy);
        va_end(args);
    }
}

//------------------------------------------------------------------------
// Print success message (green) to stdout
//------------------------------------------------------------------------
void PrintSuccess(const char* fmt, ...)
{
    if (g_hStdOut && g_hStdOut != INVALID_HANDLE_VALUE)
    {
        va_list args;
        va_start(args, fmt);
        va_list argsCopy;
        VA_COPY(argsCopy, args);
        PrintColored(g_hStdOut, FOREGROUND_GREEN | FOREGROUND_INTENSITY, fmt, argsCopy);
        va_end(argsCopy);
        va_end(args);
    }
}

//------------------------------------------------------------------------
// Print error message (red) to stderr
//------------------------------------------------------------------------
void PrintError(const char* fmt, ...)
{
    if (g_hStdErr && g_hStdErr != INVALID_HANDLE_VALUE)
    {
        va_list args;
        va_start(args, fmt);
        va_list argsCopy;
        VA_COPY(argsCopy, args);
        PrintColored(g_hStdErr, FOREGROUND_RED | FOREGROUND_INTENSITY, fmt, argsCopy);
        va_end(argsCopy);
        va_end(args);
    }
}

//------------------------------------------------------------------------
// Wait for any key press (only if both stdout and stdin are consoles)
//------------------------------------------------------------------------
void ReadKey(void)
{
    DWORD outMode, inMode;

    // If both stdout and stdin are consoles — prompt and wait for a key.
    if (g_hStdOut && GetConsoleMode(g_hStdOut, &outMode) &&
        g_hStdIn  && GetConsoleMode(g_hStdIn,  &inMode))
    {
        PrintWarning(EOL "Press any key to continue...");

        // Disable line input and echo
        SetConsoleMode(g_hStdIn, inMode & ~(ENABLE_LINE_INPUT | ENABLE_ECHO_INPUT));

        char buf;
        DWORD read;
        BOOL success = ReadFile(g_hStdIn, &buf, 1, &read, NULL) && (read == 1);

        // Restore original mode
        SetConsoleMode(g_hStdIn, inMode);
    }
    // Otherwise do nothing
}

//------------------------------------------------------------------------
// Check if a specific argument is present in the command line (case-insensitive)
//------------------------------------------------------------------------
BOOL IsArgPresent(LPCSTR lpCmdLine, LPCSTR arg)
{
    if (!lpCmdLine || !arg) return FALSE;

    int argLen = lstrlenA(arg);
    const char* p = lpCmdLine;

    while (*p)
    {
        // Skip leading spaces
        while (*p == ' ' || *p == '\t') p++;

        // Compare case-insensitively
        int i;
        for (i = 0; i < argLen; i++)
        {
            if (p[i] == '\0') break;
            char ca = p[i];
            char cb = arg[i];
            if (ca >= 'A' && ca <= 'Z') ca += 'a' - 'A';
            if (cb >= 'A' && cb <= 'Z') cb += 'a' - 'A';
            if (ca != cb) break;
        }

        if (i == argLen)
        {
            // Check if it's a full word
            char next = p[argLen];
            if (next == '\0' || next == ' ' || next == '\t')
                return TRUE;
        }

        // Move to next word
        while (*p && *p != ' ' && *p != '\t') p++;
    }

    return FALSE;
}

//------------------------------------------------------------------------
// Check administrator privileges
//------------------------------------------------------------------------
BOOL IsAdmin(void)
{
    BOOL isAdmin = FALSE;
    PSID adminGroup = NULL;
    SID_IDENTIFIER_AUTHORITY NtAuthority = SECURITY_NT_AUTHORITY;

    if (AllocateAndInitializeSid(&NtAuthority, 2,
                                  SECURITY_BUILTIN_DOMAIN_RID,
                                  DOMAIN_ALIAS_RID_ADMINS,
                                  0, 0, 0, 0, 0, 0, &adminGroup))
    {
        CheckTokenMembership(NULL, adminGroup, &isAdmin);
        FreeSid(adminGroup);
    }
    return isAdmin;
}

//------------------------------------------------------------------------
// Run a PowerShell command (hidden)
//------------------------------------------------------------------------
BOOL RunCommand(const char* command)
{
    STARTUPINFOA si = { sizeof(si) };
    PROCESS_INFORMATION pi = {0};
    char cmdLine[2048];

    // Approximate needed length: prefix + command + quotes + null
    size_t needed = 64 + lstrlenA(command);
    if (needed > sizeof(cmdLine))
    {
        PrintError("Command too long" EOL);
        return FALSE;
    }

    if (FAILED(StringCchPrintfA(cmdLine, sizeof(cmdLine)/sizeof(cmdLine[0]),
                                "powershell -NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -Command \"%s\"",
                                command)))
    {
        PrintError("FAILED to format command line" EOL);
        return FALSE;
    }

    Print("Running: %s" EOL, command);

    if (!CreateProcessA(NULL, cmdLine, NULL, NULL, FALSE,
                        CREATE_NO_WINDOW, NULL, NULL, &si, &pi))
    {
        PrintError("FAILED to start command" EOL);
        return FALSE;
    }

    DWORD wait = WaitForSingleObject(pi.hProcess, INFINITE);
    if (wait != WAIT_OBJECT_0)
    {
        PrintError("Process wait FAILED (code %lu)" EOL, wait);
        CloseHandle(pi.hProcess);
        CloseHandle(pi.hThread);
        return FALSE;
    }

    DWORD code = 0;
    GetExitCodeProcess(pi.hProcess, &code);

    CloseHandle(pi.hProcess);
    CloseHandle(pi.hThread);

    if (code != 0)
    {
        PrintError("Command FAILED (exit code %lu): %s" EOL, code, command);
        return FALSE;
    }

    return TRUE;
}

//------------------------------------------------------------------------
// Set registry DWORD value (create key if needed)
//------------------------------------------------------------------------
BOOL SetDWORD(HKEY root, const char* path, const char* name, DWORD value)
{
    HKEY key;
    if (RegCreateKeyExA(root, path, 0, NULL, 0, KEY_WRITE, NULL, &key, NULL) == ERROR_SUCCESS)
    {
        RegSetValueExA(key, name, 0, REG_DWORD, (BYTE*)&value, sizeof(value));
        RegCloseKey(key);
        Print("Registry set: %s\\%s = %lu" EOL, path, name, value);
    }
    else
    {
        PrintError("FAILED registry write: %s" EOL, path);
        return FALSE;
    }

    return TRUE;
}

//------------------------------------------------------------------------
// Reboot the system (requires admin privileges)
//------------------------------------------------------------------------
void RebootSystem(void)
{
    PrintWarning(EOL "Initiating system reboot in 10 seconds..." EOL);

    // Try modern InitiateShutdown first (Vista+)
    HANDLE hToken;
    TOKEN_PRIVILEGES tkp;

    // Get shutdown privilege
    if (OpenProcessToken(GetCurrentProcess(), TOKEN_ADJUST_PRIVILEGES | TOKEN_QUERY, &hToken))
    {
        LookupPrivilegeValue(NULL, SE_SHUTDOWN_NAME, &tkp.Privileges[0].Luid);
        tkp.PrivilegeCount = 1;
        tkp.Privileges[0].Attributes = SE_PRIVILEGE_ENABLED;
        AdjustTokenPrivileges(hToken, FALSE, &tkp, 0, NULL, NULL);
        CloseHandle(hToken);
    }

    // Initiate shutdown
    DWORD dwShutdown = InitiateSystemShutdownA(NULL, "Copilot Removal Utility - Reboot",
                                          10, // timeout in seconds
                                          SHUTDOWN_NORETRY,
                                          SHTDN_REASON_MAJOR_APPLICATION |
                                          SHTDN_REASON_MINOR_INSTALLATION |
                                          SHTDN_REASON_FLAG_PLANNED);
    if (dwShutdown == ERROR_SUCCESS)
    {
        // Success, system will reboot
        return;
    }

    // Fallback to ExitWindowsEx
    PrintError("InitiateShutdown failed (error %lu), trying ExitWindowsEx..." EOL, dwShutdown);
    ExitWindowsEx(EWX_REBOOT | EWX_FORCEIFHUNG,
                  SHTDN_REASON_MAJOR_APPLICATION |
                  SHTDN_REASON_MINOR_INSTALLATION |
                  SHTDN_REASON_FLAG_PLANNED);
}

//------------------------------------------------------------------------
// Entry point (no CRT)
//------------------------------------------------------------------------
int WINAPI WinMain(HINSTANCE hInstance, HINSTANCE hPrevInstance,
                   LPSTR lpCmdLine, int nCmdShow)
{
    g_hStdOut = GetStdHandle(STD_OUTPUT_HANDLE);
    g_hStdErr = GetStdHandle(STD_ERROR_HANDLE);
    g_hStdIn  = GetStdHandle(STD_INPUT_HANDLE);

    // Check for -r flag
    BOOL rebootRequested = IsArgPresent(lpCmdLine, "-r");

    PrintWarning("==== Copilot removal utility ====" EOL EOL);

    if (!IsAdmin())
    {
        PrintError("ERROR: Administrator rights required." EOL);
        PrintError("Run program as Administrator." EOL);
        ReadKey();
        return 1;
    }

    PrintSuccess("Administrator privileges confirmed." EOL EOL);

    Print("Removing Copilot packages..." EOL);
    BOOL success = TRUE;
    success &= RunCommand("Get-AppxPackage *copilot* | Remove-AppxPackage");
    success &= RunCommand("Get-AppxPackage -AllUsers *copilot* | Remove-AppxPackage");
    success &= RunCommand("Get-AppxProvisionedPackage -Online | where DisplayName -like '*copilot*' | Remove-AppxProvisionedPackage -Online");

    Print("Applying system policies..." EOL);
    success &= SetDWORD(HKEY_CURRENT_USER,
             "Software\\Policies\\Microsoft\\Windows\\WindowsCopilot",
             "TurnOffWindowsCopilot", 1);
    success &= SetDWORD(HKEY_LOCAL_MACHINE,
             "SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsCopilot",
             "TurnOffWindowsCopilot", 1);
    success &= SetDWORD(HKEY_CURRENT_USER,
             "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced",
             "ShowCopilotButton", 0);

    if (success)
    {
        PrintSuccess(EOL "Operation finished successfully." EOL);
    }
    else
    {
        PrintError(EOL "One or more errors occurred during operation." EOL);
    }
    PrintWarning("Reboot Windows to apply all changes." EOL);

    if (rebootRequested && success)
    {
        RebootSystem();
        // The system will reboot, so we don't need to wait for a key
        return 0;
    }

    ReadKey();
    return 0;
}