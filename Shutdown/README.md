# Description
These are three small tools for Windows that are analogs of the standard shutdown command without displaying a console window. The program takes one command line argument that specifies the type of action to perform: shutdown, reboot, quit applications, log off, exit hybrid mode. The program accesses the process token to request the shutdown privilege, and then uses the ExitWindowsEx function to perform the selected action.

## Usage
1. **exitwin.exe** program runs the action specified by the command line parameter:

    |   Key  | Action       |  
    | :----: | :----------- |
    |    s   | Shut down    |
    |    r   | Reboot       |
    |    a   | Restart apps |
    |    l   | Logging off  |
    |    h   | Hybernate    |

3. **halt.exe** - shut down (equivalent of "**exitwin.exe s**").
4. **reboot.exe** - reboot (equivalent of "**exitwin.exe r**").

# How it works

The code provided in the source code demonstrates the implementation of system shutdown using the command line:

- Getting command line arguments: The program receives the arguments passed to it via the command line. In this case, it is the szCmdLine string.
- Processing arguments: The code analyzes the passed argument. In the example, the program checks whether the argument is a single character and, depending on the value of this character, performs a certain action (shutdown, reboot, end session, etc.).
- Privilege escalation: To perform system operations such as shutdown or reboot, the program requires special privileges. The code uses the OpenProcessToken, LookupPrivilegeValue, and AdjustTokenPrivileges functions to obtain the necessary privileges.
- Calling the ExitWindowsEx function: The ExitWindowsEx function is used to perform the selected operation (shutdown, reboot, etc.).

## Program reaction to missing command line argument

If the command line argument is missing, the program will perform the following actions:
- Checking for the presence of an argument: In the WinMain function, it is checked whether szCmdLine (a pointer to the command line argument string) is NULL or an empty string (*szCmdLine == L'\0').
- Outputting an error message: If the argument is missing, the message "No command line argument specified." is displayed using the MessageBox function.
- Exiting: The program exits with error code 1.

Thus, the program does not perform any actions related to system shutdown, but informs the user about the missing argument and exits.

## What the program does when the command line argument is invalid

If the program receives an invalid command line argument, it does the following:
- Checks the argument length: If the argument length is not 1 character, the program displays the message "Invalid command line argument." using the MessageBox function and exits with error code 2.
- Checks the argument value: If the argument length is 1 character, the program checks its value using the switch statement. If the value does not match any of the valid options ('s', 'r', 'a', 'l', 'h'), the program also displays the message "Invalid command line argument." using the MessageBox function and exits with error code 2.
It is important to note that in both cases:
- Before displaying the error message, the program frees the memory allocated for the szCmdLower variable using the free function.
- Error code 2 indicates an invalid command line argument.

Thus, the program handles invalid command line arguments by displaying an error message and exiting with an appropriate error code.

## Error handling

The error handling in the code provided is not ideal. Only MessageBox is used to display error messages.
