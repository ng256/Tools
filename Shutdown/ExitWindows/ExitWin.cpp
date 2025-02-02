/***********************************************************
This program allows the user  to perform system shutdown  or 
reboot  actions.   It  uses  Windows  API  calls  to  adjust 
privileges and execute the requested action.

Command line  arguments  are  used  to specify  the  desired 
action:

  - '/s' for shutdown
  - '/r' for reboot
  - '/a' for restarting apps
  - '/l' for logoff
  - '/h' for hybrid shutdown

Distributed under MIT License:                             

Copyright (c) 2024 Pavel Bashkardin

Permission is hereby granted,  free of charge, to any person
obtaining   a  copy   of    this  software    and associated
documentation  files  (the    "Software"),   to deal  in the
Software without restriction,  including without  limitation
the rights to use, copy, modify, merge, publish, distribute,
sublicense,  and/or sell copies   of the  Software,   and to
permit  persons to whom  the Software is furnished to do so,
subject to the following conditions:

1.  The above copyright  notice and this   permission notice
shall be included  in  all copies or substantial portions of
the Software.

2. THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY
KIND, EXPRESS OR IMPLIED, INCLUDING  BUT NOT LIMITED  TO THE
WARRANTIES  OF MERCHANTABILITY,  FITNESS   FOR  A PARTICULAR
PURPOSE AND NONINFRINGEMENT. IN  NO EVENT SHALL  THE AUTHORS
OR  COPYRIGHT  HOLDERS  BE LIABLE  FOR ANY CLAIM, DAMAGES OR
OTHER LIABILITY, WHETHER IN AN  ACTION OF CONTRACT,  TORT OR
OTHERWISE,  ARISING FROM, OUT OF  OR  IN CONNECTION WITH THE
SOFTWARE  OR THE  USE   OR OTHER DEALINGS IN   THE SOFTWARE.
***********************************************************/

#include <windows.h>      // Windows API header for core functions and data types
#include <winuser.h>      // Windows User API header for user interface functions
#include <cwchar>         // Header for wide character functions

#define ERR_T L"Error"                        // Title for error messages
#define MB_OK_ERROR (MB_OK | MB_ICONERROR)   // MessageBox style for error messages

// Entry point for a Windows application
int WINAPI WinMain(HINSTANCE hInstance, HINSTANCE hPrevInstance, LPWSTR szCmdLine, int iCmdShow)
{
    UINT ewxFlags = 0;                // Variable to hold exit window flags
    HANDLE hToken;                    // Handle to the access token of the current process
    TOKEN_PRIVILEGES tkp;            // Structure to hold the privileges for the token

    // Check if command line argument is provided
    if (szCmdLine == NULL || *szCmdLine == L'\0')
    {
        MessageBox(NULL, L"No command line argument specified.", ERR_T, MB_OK_ERROR);
        return 1; // Error code for no arguments
    }

    // Get the length of the command line
    size_t len = wcslen(szCmdLine);
    wchar_t* szCmdLower = static_cast<wchar_t*>(malloc((len + 1) * sizeof(wchar_t))); // Allocate memory for lowercase command
    if (!szCmdLower) {
        MessageBox(NULL, L"Memory allocation failed.", ERR_T, MB_OK_ERROR);
        return 5; // Error code for memory allocation failure
    }

    // Copy command line and convert to lowercase
    wcscpy_s(szCmdLower, len + 1, szCmdLine);
    for (size_t i = 0; i < len; i++)
        szCmdLower[i] = towlower(szCmdLower[i]);

    // Validate the command line argument and set corresponding exit flags
    if (len == 1)
    {
        switch (szCmdLower[0])
        {
        case L's':
            ewxFlags = EWX_SHUTDOWN;          // Shutdown
            break;
        case L'r':
            ewxFlags = EWX_REBOOT;            // Reboot
            break;
        case L'a':
            ewxFlags = EWX_RESTARTAPPS;       // Restart apps after shutdown
            break;
        case L'l':
            ewxFlags = EWX_LOGOFF;            // Log off
            break;
        case L'h':
            ewxFlags = EWX_HYBRID_SHUTDOWN;   // Hybrid shutdown
            break;
        default:
            MessageBox(NULL, L"Invalid command line argument.", ERR_T, MB_OK_ERROR);
            free(szCmdLower);
            return 2; // Error code for invalid argument
        }
    }
    else
    {
        MessageBox(NULL, L"Invalid command line argument.", ERR_T, MB_OK_ERROR);
        free(szCmdLower);
        return 2; // Error code for invalid argument
    }

    free(szCmdLower); // Free the allocated memory for the command line

    // Open the process token with necessary privileges
    if (!OpenProcessToken(GetCurrentProcess(), TOKEN_ADJUST_PRIVILEGES | TOKEN_QUERY, &hToken))
    {
        MessageBox(NULL, L"Failed to access process token.", ERR_T, MB_OK_ERROR);
        return 3; // Error code for failure to access token
    }

    // Retrieve the LUID (Locally Unique Identifier) for the shutdown privilege
    if (!LookupPrivilegeValue(NULL, SE_SHUTDOWN_NAME, &tkp.Privileges[0].Luid)) {
        MessageBox(NULL, L"Failed to lookup privilege value.", ERR_T, MB_OK_ERROR);
        return 3; // Error code for failure to lookup privilege
    }
    tkp.PrivilegeCount = 1; // Set the number of privileges to be adjusted
    tkp.Privileges[0].Attributes = SE_PRIVILEGE_ENABLED; // Enable the shutdown privilege

    // Adjust the token privileges to enable the shutdown privilege
    AdjustTokenPrivileges(hToken, FALSE, &tkp, 0, (PTOKEN_PRIVILEGES)nullptr, 0);
    if (GetLastError() != ERROR_SUCCESS) {
        MessageBox(NULL, L"Failed to adjust token privileges.", ERR_T, MB_OK_ERROR);
        return 3; // Error code for failure to adjust privileges
    }

    // Attempt to exit Windows based on the specified flags
    if (ExitWindowsEx(ewxFlags, 0) == 0)
    {
        MessageBox(NULL, L"Shutdown cannot be initiated.", ERR_T, MB_OK_ERROR);
        return 4; // Error code for failure to initiate shutdown
    }

    return 0; // Successful execution
}
