import ctypes
import sys

# ***********************************************************
# This program demonstrates a method to initiate a system
# reboot on Windows by adjusting the process's privileges
# to enable the required shutdown privilege. It first 
# retrieves the current process's token and elevates its 
# privileges by enabling "SeShutdownPrivilege" using native 
# Windows API calls. Once the privilege is granted, the 
# program attempts to initiate a system reboot.
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
EWX_REBOOT = 0x00000002

# Structure for token privileges
class TOKEN_PRIVILEGES(ctypes.Structure):
    _fields_ = [("PrivilegeCount", ctypes.c_ulong),
                ("Privileges", ctypes.c_void_p)]

# Function to enable shutdown privileges
def enable_shutdown_privilege():
    # Open the process token
    hToken = ctypes.c_void_p()
    if not ctypes.windll.advapi32.OpenProcessToken(
            ctypes.windll.kernel32.GetCurrentProcess(),
            TOKEN_ADJUST_PRIVILEGES | TOKEN_QUERY,
            ctypes.byref(hToken)):
        print("Failed to access process token.")
        return False

    # Lookup privilege value
    luid = ctypes.c_ulonglong()
    if not ctypes.windll.advapi32.LookupPrivilegeValueW(None, SE_SHUTDOWN_NAME, ctypes.byref(luid)):
        print("Failed to lookup privilege value.")
        return False

    # Prepare token privileges structure
    tkp = TOKEN_PRIVILEGES()
    tkp.PrivilegeCount = 1
    tkp.Privileges = ctypes.pointer(luid)

    # Enable the privilege
    tkp.Privileges[0] = ctypes.c_ulonglong(luid.value | SE_PRIVILEGE_ENABLED)
    ctypes.windll.advapi32.AdjustTokenPrivileges(hToken, False, ctypes.byref(tkp), 0, None, None)

    # Check if the privilege was adjusted successfully
    if ctypes.windll.kernel32.GetLastError() != 0:
        print("Failed to adjust token privileges.")
        return False

    return True

# Entry point
if __name__ == "__main__":
    if enable_shutdown_privilege():
        # Initiate reboot
        if ctypes.windll.user32.ExitWindowsEx(EWX_REBOOT, 0) == 0:
            print("Reboot cannot be initiated.")
            sys.exit(1)
