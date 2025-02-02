import ctypes
import sys

# ***********************************************************
# This program allows the user to perform system shutdown or 
# reboot actions. It uses Windows API calls to adjust 
# privileges and execute the requested action.
#
# Command line arguments are used to specify the desired 
# action:
#
#   - '/s' for shutdown
#   - '/r' for reboot
#   - '/a' for restarting apps
#   - '/l' for logoff
#   - '/h' for hybrid shutdown
#
# Distributed under MIT License:                             
# 
# Copyright (c) 2024 Pavel Bashkardin
#
# Permission is hereby granted, free of charge, to any person
# obtaining a copy of this software and associated documentation
# files (the "Software"), to deal in the Software without 
# restriction, including without limitation the rights to use, 
# copy, modify, merge, publish, distribute, sublicense, and/or 
# sell copies of the Software, and to permit persons to whom 
# the Software is furnished to do so, subject to the following 
# conditions:
#
# 1. The above copyright notice and this permission notice 
# shall be included in all copies or substantial portions of 
# the Software.
#
# 2. THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY
# KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
# WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
# PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS
# OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR 
# OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
# OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE 
# SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
# ***********************************************************

# Constants
TOKEN_ADJUST_PRIVILEGES = 0x0020
TOKEN_QUERY = 0x0008
SE_SHUTDOWN_NAME = "SeShutdownPrivilege"
SE_PRIVILEGE_ENABLED = 0x0002
EWX_SHUTDOWN = 0x00000001
EWX_REBOOT = 0x00000002
EWX_RESTARTAPPS = 0x00000040
EWX_LOGOFF = 0x00000000
EWX_HYBRID_SHUTDOWN = 0x00400000

# Structure for token privileges
class TOKEN_PRIVILEGES(ctypes.Structure):
    _fields_ = [("PrivilegeCount", ctypes.c_ulong),
                ("Privileges", ctypes.c_void_p)]

# Function to enable shutdown privileges
def enable_shutdown_privilege():
    hToken = ctypes.c_void_p()
    if not ctypes.windll.advapi32.OpenProcessToken(
            ctypes.windll.kernel32.GetCurrentProcess(),
            TOKEN_ADJUST_PRIVILEGES | TOKEN_QUERY,
            ctypes.byref(hToken)):
        print("Failed to access process token.")
        return False

    luid = ctypes.c_ulonglong()
    if not ctypes.windll.advapi32.LookupPrivilegeValueW(None, SE_SHUTDOWN_NAME, ctypes.byref(luid)):
        print("Failed to lookup privilege value.")
        return False

    tkp = TOKEN_PRIVILEGES()
    tkp.PrivilegeCount = 1
    tkp.Privileges = ctypes.pointer(luid)

    tkp.Privileges[0] = ctypes.c_ulonglong(luid.value | SE_PRIVILEGE_ENABLED)
    ctypes.windll.advapi32.AdjustTokenPrivileges(hToken, False, ctypes.byref(tkp), 0, None, None)

    if ctypes.windll.kernel32.GetLastError() != 0:
        print("Failed to adjust token privileges.")
        return False

    return True

if __name__ == "__main__":

    if len(sys.argv) < 2:
        print("No command line argument specified.")
        sys.exit(1)  # Error code for no arguments

    argument = sys.argv[1].lower()
    ewx_flags = 0

    # Validate the command line argument and set corresponding exit flags
    if argument == '/s':
        ewx_flags = EWX_SHUTDOWN  # Shutdown
    elif argument == '/r':
        ewx_flags = EWX_REBOOT  # Reboot
    elif argument == '/a':
        ewx_flags = EWX_RESTARTAPPS  # Restart apps after shutdown
    elif argument == '/l':
        ewx_flags = EWX_LOGOFF  # Log off
    elif argument == '/h':
        ewx_flags = EWX_HYBRID_SHUTDOWN  # Hybrid shutdown
    else:
        print("Invalid command line argument.")
        sys.exit(2)  # Error code for invalid argument

    # Open the process token with necessary privileges
    if not enable_shutdown_privilege():
        sys.exit(3)  # Error code for failure to adjust privileges

    # Attempt to exit Windows based on the specified flags
    if ctypes.windll.user32.ExitWindowsEx(ewx_flags, 0) == 0:
        print("Shutdown cannot be initiated.")
        sys.exit(4)  # Error code for failure to initiate shutdown

    sys.exit(0)  # Successful execution
