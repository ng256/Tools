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

Permission    is  hereby  granted,   free of charge,  to any
person  obtaining a copy of this   software   and associated
documentation  files  (the    "Software"),   to deal  in the
Software  without  restriction, including without limitation
the   rights to  use,   copy,  modify,   merge,     publish,
distribute,    sublicense,    and/or   sell  copies   of the
Software,  and  to   permit  persons  to   whom the Software
is furnished to  do so, subject to the following conditions:

The  above  copyright  notice  and  this  permission  notice
shall  be included in  all copies or substantial portions of
the Software.

THE   SOFTWARE  IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY
KIND, EXPRESS OR IMPLIED, INCLUDING   BUT NOT LIMITED TO THE
WARRANTIES  OF MERCHANTABILITY, FITNESS    FOR A  PARTICULAR
PURPOSE AND NONINFRINGEMENT. IN  NO EVENT SHALL  THE AUTHORS
OR COPYRIGHT HOLDERS   BE  LIABLE FOR ANY CLAIM,  DAMAGES OR
OTHER  LIABILITY,  WHETHER IN AN ACTION OF CONTRACT, TORT OR
OTHERWISE, ARISING FROM, OUT OF   OR IN CONNECTION  WITH THE
SOFTWARE  OR THE USE  OR  OTHER  DEALINGS IN   THE SOFTWARE.
***********************************************************/

using System;
using System.Runtime.InteropServices;

class Program
{
    // Constants defining specific privileges and flags used for shutdown
    const uint TokenAdjustPrivileges = 0x0020; // Allows modifying privileges in a token
    const uint TokenQuery = 0x0008;            // Allows querying a token
    const uint SePrivilegeEnabled = 0x0002;    // Enables a privilege
    const uint RebootFlags = 0x0001;           // Specifies a reboot operation
    const uint MbIconError = 0x00000010;       // Icon for error message in MessageBox
    const string SeShutdownName = "SeShutdownPrivilege"; // Privilege name required for shutdown

    // Struct for Local Unique Identifier (LUID) for a privilege
    [StructLayout(LayoutKind.Sequential)]
    struct LUID
    {
        public uint LowPart;  // Low 32 bits of LUID
        public int HighPart;  // High 32 bits of LUID
    }

    // Struct for token privileges to be set on a process
    [StructLayout(LayoutKind.Sequential)]
    struct TokenPrivileges
    {
        public uint PrivilegeCount; // Number of privileges in the array
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
        public LUID[] Privileges;    // Array of privileges (only one needed here)

        // Constructor initializing PrivilegeCount and setting the Privileges array to size 1
        public TokenPrivileges(uint privilegeCount)
        {
            PrivilegeCount = privilegeCount;
            Privileges = new LUID[1];
        }
    }

    // Import of the Windows API function for displaying a message box
    [DllImport("user32.dll", SetLastError = true)]
    static extern bool MessageBox(IntPtr hWnd, string lpText, string lpCaption, uint uType);

    // Import of AdjustTokenPrivileges to enable or disable specified privileges
    [DllImport("advapi32.dll", SetLastError = true)]
    static extern bool AdjustTokenPrivileges(IntPtr hToken, 
					     bool disableAllPrivileges, 
					     ref TokenPrivileges newPrivileges, 
					     uint sizeOfPreviousPrivileges, 
					     IntPtr previousPrivileges, 
					     uint sizeOfTokenInformation);

    // Import of GetCurrentProcess to obtain a handle to the current process
    [DllImport("kernel32.dll")]
    static extern IntPtr GetCurrentProcess();

    // Import of OpenProcessToken to open the token associated with a process
    [DllImport("advapi32.dll")]
    static extern bool OpenProcessToken(IntPtr processHandle, uint desiredAccess, out IntPtr tokenHandle);

    // Import of LookupPrivilegeValue to get the LUID for a specified privilege
    [DllImport("advapi32.dll")]
    static extern bool LookupPrivilegeValue(string lpSystemName, string lpName, out LUID lpLuid);

    // Import of ExitWindowsEx to initiate a shutdown/restart operation
    [DllImport("user32.dll")]
    static extern uint ExitWindowsEx(uint uFlags, uint dwReason);

    // Main method to initiate shutdown with required privilege elevation
    static int Main(string[] args)
    {
        IntPtr hToken; // Handle to the process token
        LUID luid;     // LUID for the shutdown privilege

        // Open the current process token with permissions to adjust privileges
        if (!OpenProcessToken(GetCurrentProcess(), TokenAdjustPrivileges | TokenQuery, out hToken))
        {
            MessageBox(IntPtr.Zero, "Failed to access process token.", "Error", MbIconError);
            return 1;
        }

        // Lookup the LUID for shutdown privilege
        if (!LookupPrivilegeValue(null, SeShutdownName, out luid))
        {
            MessageBox(IntPtr.Zero, "Failed to lookup privilege value.", "Error", MbIconError);
            return 1;
        }

        // Create a TokenPrivileges structure and assign shutdown privilege
        TokenPrivileges tkp = new TokenPrivileges(1);
        tkp.Privileges[0] = luid;
        tkp.Privileges[0].HighPart = SePrivilegeEnabled;

        // Enable shutdown privilege in the token
        if (!AdjustTokenPrivileges(hToken, false, ref tkp, 0, IntPtr.Zero, 0))
        {
            MessageBox(IntPtr.Zero, "Failed to adjust token privileges.", "Error", MbIconError);
            return 1;
        }

        // Attempt to initiate system reboot
        if (ExitWindowsEx(RebootFlags, 0) == 0)
        {
            MessageBox(IntPtr.Zero, "Reboot cannot be initiated.", "Error", MbIconError);
            return 1;
        }

        return 0;
    }
}
