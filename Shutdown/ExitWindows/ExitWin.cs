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
    /*
     * This program allows the user to perform system shutdown or reboot actions.
     * It uses Windows API calls to adjust privileges and execute the requested action.
     * Command line arguments are used to specify the desired action:
     *   - '/s' for shutdown
     *   - '/r' for reboot
     *   - '/a' for restarting apps
     *   - '/l' for logoff
     *   - '/h' for hybrid shutdown
     * 
     * Note: Administrative privileges are required to execute shutdown or reboot actions.
     */

    const uint TokenAdjustPrivileges = 0x0020;      // Allows modifying privileges in a token
    const uint TokenQuery = 0x0008;                 // Allows querying a token
    const uint SePrivilegeEnabled = 0x0002;         // Enables a privilege
    const string SeShutdownName = "SeShutdownPrivilege"; // Privilege name required for shutdown

    const uint MbOk = 0;                           // Represents a message box with an OK button
    const uint MbIconError = 0x00000010;           // Icon for error message in MessageBox

    [Flags]
    public enum ExitFlags : uint
    {
        Shutdown = 0x0001,        // Shutdown the system
        Reboot = 0x0002,          // Reboot the system
        RestartApps = 0x0040,     // Restart applications after shutdown
        Logoff = 0x0000,          // Log off the user
        HybridShutdown = 0x0020   // Hybrid shutdown (sleep and hibernate)
    }

    [StructLayout(LayoutKind.Sequential)]
    struct LUID
    {
        public uint LowPart;  // Low 32 bits of LUID
        public int HighPart;  // High 32 bits of LUID
    }

    [StructLayout(LayoutKind.Sequential)]
    struct TokenPrivileges
    {
        public uint PrivilegeCount; // Number of privileges in the array
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
        public LUID[] Privileges;    // Array of privileges (only one needed here)

        public TokenPrivileges(uint privilegeCount)
        {
            PrivilegeCount = privilegeCount;  // Set the number of privileges
            Privileges = new LUID[1];         // Initialize the privileges array
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

    // Import of ExitWindowsEx to initiate a shutdown/reboot operation
    [DllImport("user32.dll")]
    static extern uint ExitWindowsEx(ExitFlags uFlags, uint dwReason);

    static int Main(string[] args)
    {
        // Check if any command line arguments are provided
        if (args.Length == 0)
        {
            MessageBox(IntPtr.Zero, "No command line argument specified.", 
		       "Error", MbOk | MbIconError);
            return 1; // Exit with error code
        }

        // Validate the number of command line arguments
        if (args.Length > 1)
        {
            MessageBox(IntPtr.Zero, "Invalid number of command line arguments specified.", 
		       "Error", MbOk | MbIconError);
            return 2;
        }
		
		// Getting the first argument
		string arg = args[0];

        // Check the length of the first command line argument
        if (arg.Length > 2 || !arg.StartsWith('/'))
        {
            MessageBox(IntPtr.Zero, "Invalid command line argument specified." +
		       " Use /s, /r, /a, /l, or /h.", 
		       "Error", MbOk | MbIconError);
            return 2;
        }

        // Get the character after '/' for case-insensitive comparison
        char cmdArg = arg.ToLower()[1];

        ExitFlags exitFlags; // Variable to hold the desired exit action

        // Determine the action based on the command line argument
        switch (cmdArg)
        {
            case 's':
                exitFlags = ExitFlags.Shutdown; // Shutdown
                break;
            case 'r':
                exitFlags = ExitFlags.Reboot; // Reboot
                break;
            case 'a':
                exitFlags = ExitFlags.RestartApps; // Restart applications
                break;
            case 'l':
                exitFlags = ExitFlags.Logoff; // Logoff
                break;
            case 'h':
                exitFlags = ExitFlags.HybridShutdown; // Hybrid shutdown
                break;
            default:
                // Invalid command line argument specified
                MessageBox(IntPtr.Zero, "Invalid command line argument specified. " +
			   "Use /s, /r, /a, /l, or /h.", 
			   "Error", MbOk | MbIconError);
                return 2;
        }

        IntPtr hToken; // Handle to the process token
        LUID luid; // LUID structure to hold the privilege identifier

        // Open the process token with necessary privileges
        if (!OpenProcessToken(GetCurrentProcess(), TokenAdjustPrivileges | TokenQuery, out hToken))
        {
            MessageBox(IntPtr.Zero, "Failed to access process token.", 
		       "Error", MbOk | MbIconError);
            return 3;
        }

        // Lookup the LUID for the shutdown privilege
        if (!LookupPrivilegeValue(null, SeShutdownName, out luid))
        {
            MessageBox(IntPtr.Zero, "Failed to lookup privilege value.", "Error", MbOk | MbIconError);
            return 3;
        }

        TokenPrivileges tkp = new TokenPrivileges(1); // Create a TokenPrivileges structure
        tkp.Privileges[0] = luid; // Set the privilege
        tkp.Privileges[0].HighPart = SePrivilegeEnabled; // Enable the privilege

        // Adjust the token privileges
        if (!AdjustTokenPrivileges(hToken, false, ref tkp, 0, IntPtr.Zero, 0))
        {
            MessageBox(IntPtr.Zero, "Failed to adjust token privileges.", 
		       "Error", MbOk | MbIconError);
            return 3;
        }

        // Attempt to exit the Windows based on the specified flags
        if (ExitWindowsEx(exitFlags, 0) == 0)
        {
            MessageBox(IntPtr.Zero, "Shutdown cannot be initiated.", 
		       "Error", MbOk | MbIconError);
            return 4;
        }

        return 0; // Successful execution
    }
}
