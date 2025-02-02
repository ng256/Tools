/***********************************************************
This   program  demonstrates    a  method  to    initiate  a
system  reboot  on  Windows   by   adjusting  the  process's
privileges  to  enable     the required  shutdown privilege.
It   first  retrieves  the  current   process's    token and
elevates  its  privileges  by enabling "SeShutdownPrivilege"
using native  Windows API calls.  Once   the  privilege   is
granted,   the  program  attempts  to   initiate   a  system
reboot.

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

#include <windows.h>
#include <winuser.h>

// Entry point for a Windows application
int WINAPI WinMain(HINSTANCE hInstance, HINSTANCE hPrevInstance, PSTR szCmdLine, int iCmdShow)
{
    HANDLE hToken;                       // Handle to the access token of the current process
    TOKEN_PRIVILEGES tkp;                // Structure to hold the privileges for the token

    // Open the process token for the current process with the specified access rights
    OpenProcessToken(GetCurrentProcess(), TOKEN_ADJUST_PRIVILEGES | TOKEN_QUERY, &hToken);

    // Retrieve the LUID (Locally Unique Identifier) for the shutdown privilege
    LookupPrivilegeValue(NULL, SE_SHUTDOWN_NAME, &tkp.Privileges[0].Luid);

    // Set the number of privileges to be adjusted
    tkp.PrivilegeCount = 1;

    // Enable the shutdown privilege in the token
    tkp.Privileges[0].Attributes = SE_PRIVILEGE_ENABLED;

    // Adjust the token privileges to enable the shutdown privilege
    AdjustTokenPrivileges(hToken, FALSE, &tkp, 0, (PTOKEN_PRIVILEGES)NULL, 0);

    // Initiate a system reboot; return non-zero if successful
    return ExitWindowsEx(EWX_REBOOT, 0) != 0;
}
