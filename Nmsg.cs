/*
 * nmsg.exe - Simple notification tool for displaying customizable notifications.
 * 
 * Usage: nmsg.exe [message]
 * 
 * Displays a notification message with options for duration, icon, and whether 
 * to wait for user action before closing the notification.
 * 
 * Options:
 *  - Duration: The duration of the notification in milliseconds (default: 5000ms)
 *  - Icon: Type of icon (default: Info). Options: "Information", "Warning", "Error".
 *  - Await: Whether to wait for user action before closing the notification (default: false).
 *  - DefaultMessage: Message to show if no argument is passed (default: Help message).
 * 
 * Example of a config.ini file:
 * 
 * [Notification]
 * Duration = 5000
 * NotifyIcon = Information  ; Possible values: "Information", "Warning", "Error"
 * Await = false
 * ; DefaultMessage = "This is the default notification message. You can customize it here."
 * 
 * MIT License
 * 
 * Copyright (c) 2024 Pavel Pashkardin
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
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
 * FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS
 * IN THE SOFTWARE.
 */

using System;
using System.Windows.Forms;
using System.Ini; // The namespace for the IniFile class. For details, check out the GitHub page at https://github.com/ng256/IniFile.

class Program
{
    // Define constants
    private const string ConfigFilePath = "config.ini"; // Path to the configuration file
    private const int DefaultDuration = 5000; // Default duration for the notification in milliseconds
    private const string HelpMessage = "Usage: nmsg.exe [message]\n\nDisplays a notification message.\n\nOptions:\n  - Duration: The duration of the notification (default: 5000ms)\n  - Icon: Type of icon (default: Info)\n  - Await: Whether to wait for user action before closing (default: false)"; // Help message
    private const int SustainDelay = 500; // Additional sustain delay in milliseconds after the notification to ensure proper completion

    static void Main(string[] args)
    {
        try
        {
            // Load or create the INI file with parameters if needed
            IniFile config = IniFile.LoadOrCreate(ConfigFilePath);

            // Read parameters from the "Notification" section
            int duration = config.ReadInt32("Notification", "Duration", DefaultDuration);  // Use DefaultDuration if not specified
            string iconType = config.ReadString("Notification", "NotifyIcon", "Information");  // Notification icon type
            bool awaitUserAction = config.ReadBool("Notification", "Await", false);  // Wait for user action
            string defaultMessage = config.ReadString("Notification", "DefaultMessage", HelpMessage);  // Use HelpMessage if not specified

            // Use the help message if no text is provided via the command line
            string message = args.Length > 0 ? string.Join(" ", args) : defaultMessage;

            // Handle iconType and choose the appropriate ToolTipIcon
            ToolTipIcon toolTipIcon;
            switch (iconType.ToLower())
            {
                case "warning":
                    toolTipIcon = ToolTipIcon.Warning;
                    break;
                case "error":
                    toolTipIcon = ToolTipIcon.Error;
                    break;
                default:
                    toolTipIcon = ToolTipIcon.Info;
                    break;
            }

            // Show the notification
            ShowNotification(toolTipIcon, duration, message);

            // If the "Await" parameter is true, wait for the user to close the notification
            if (awaitUserAction)
            {
                // Wait for the user to dismiss the notification
                Application.Run();
            }
            else
            {
                // Pause to ensure the notification is shown before the program exits
                System.Threading.Thread.Sleep(duration + SustainDelay); // Add the sustain delay after the notification
            }
        }
        catch (Exception ex)
        {
            // General error handling block
            string errorMessage = "An unexpected error occurred: " + ex.Message;

            // Show an error notification
            ShowNotification(ToolTipIcon.Error, DefaultDuration, errorMessage);
        }
    }

    // Method to display notifications
    static void ShowNotification(ToolTipIcon toolTipIcon, int duration, string message)
    {
        using (NotifyIcon notifyIcon = new NotifyIcon())
        {
            notifyIcon.Icon = SystemIcons.Information; // Set the default icon
            notifyIcon.Visible = true;

            // Display the notification
            notifyIcon.ShowBalloonTip(duration, "Notification", message, toolTipIcon);

            // Pause to ensure the notification is shown before the program exits
            System.Threading.Thread.Sleep(duration + SustainDelay); // Add the sustain delay after the notification
        }
    }
}
