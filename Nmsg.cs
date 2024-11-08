/*
 * nmsg.exe - Simple notification tool for displaying customizable notifications.
 * 
 * Usage: nmsg.exe [options]
 * 
 * Options:
 *  -title <title>      Sets the title of the notification (default: "Notification").
 *  -message <message>  Sets the message of the notification.
 *  -duration <ms>      Sets the display duration of the notification in seconds (default: 5).
 *  -warning            Sets the icon to "Warning".
 *  -error              Sets the icon to "Error".
 * 
 * Config file format (config.ini):
 * 
 * [Settings]
 * Duration = 5              ; Notification duration in seconds
 * Icon = 0                  ; 0 = Information, 1 = Warning, 2 = Error
 * Title = Notification      ; Default notification title
 * Message = Default message ; Default notification message
 * 
 * If the configuration file is present, its settings are used as defaults,
 * which can be overridden by command-line arguments.
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
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using System.Ini; // The namespace for the IniFile class. For details, check out the GitHub page at https://github.com/ng256/IniFile.

[IniSection("Settings")]
class Notification
{
    /***** Default values. ***********************************************************************************************************/

    // Default notification duration in seconds if not specified in settings or arguments.
    internal const int DefaultDuration = 5;

    // Help message, shown as the notification message if not overridden.
    internal const string HelpMessage = "Usage: nmsg.exe [options]\n" +
                                        "Displays a notification message.\n" +
                                        "Options:\n" +
                                        "  -title <title>      Sets the title of the notification\n" +
                                        "  -message <message>  Sets the message of the notification\n" +
                                        "  -duration <ms>      Sets the display duration of the notification in seconds (default: 5)" +
                                        "  -warning            Sets the icon to \"Warning\"" +
                                        "  -error              Sets the icon to \"Error\"";

    // Default notification title, used if not specified elsewhere.
    internal const string DefaultTitle = "Notification";

    /***** Parameters that can be obtained via command-line argument or INI file. ****************************************************/

    // Duration of the notification in seconds.
    public int Duration { get; set; } = DefaultDuration;

    // Icon type for the notification (e.g., Info, Warning, Error).
    public ToolTipIcon Icon { get; set; } = ToolTipIcon.Info;

    // Notification message text.
    public string Message { get; set; } = HelpMessage;

    // Notification title text.
    public string Title { get; set; } = DefaultTitle;

    /***** Shows the notification with the specified settings. ************************************************************************/

    public void Show()
    {
        Show(Duration, Title, Message, Icon);
    }

    public static void Show(int duration, string title, string message, ToolTipIcon toolTipIcon)
    {
        const int sustainDelay = 500; // Additional sustain delay in milliseconds after the notification to ensure proper completion
        duration *= 1000;

        using (NotifyIcon notifyIcon = new NotifyIcon())
        {
            notifyIcon.Icon = SystemIcons.Information; // Set the default icon
            notifyIcon.Visible = true;
            notifyIcon.Text = message.Length < 64 ? message : message.Substring(0, 63);
            // Display the notification
            notifyIcon.ShowBalloonTip(duration, title, message, toolTipIcon);
            System.Threading.Thread.Sleep(duration + sustainDelay); // Add the sustain delay after the notification
        }
    }
}

static class Program
{
    [STAThread]
    static int Main(string[] args)
    {
        try
        {
            // Path to the configuration file
            const string configFilePath = "config.ini"; 

            // Load the configuration file.
            IniFile config = IniFile.LoadOrCreate(configFilePath);

            // Read parameters from the "Settings" section.
            Notification notification = new Notification();
            config.ReadSettings(notification);

            // Value to override the default icon.
            ToolTipIcon icon = ToolTipIcon.None;

            for (var i = 0; i < args.Length; i++)
            {
                string arg = args[i];

                // Process arguments prefixed with "-" or "/".
                if (arg.StartsWith("-") || arg.StartsWith("/"))
                {
                    bool notLast = i < args.Length - 1;

                    switch (arg.TrimStart('-', '/').ToLowerInvariant())
                    {
                        case "title" when notLast:
                            notification.Title = args[++i];
                            break;
                        case "message" when notLast:
                            notification.Message = args[++i];
                            break;
                        case "duration" when notLast
                                             && int.TryParse(args[++i],
                                                 NumberStyles.Integer,
                                                 CultureInfo.InvariantCulture,
                                                 out int duration):
                            notification.Duration = duration;
                            break;
                        case "warning" when icon == ToolTipIcon.None:
                            icon = ToolTipIcon.Warning;
                            break;
                        case "error" when icon == ToolTipIcon.None:
                            icon = ToolTipIcon.Error;
                            break;
                    }
                }
            }

            // Apply icon override if specified by arguments.
            if (icon > ToolTipIcon.None) 
                notification.Icon = icon;

            // Show the notification
            notification.Show();

            return 0; // Exit code 0 indicates success.
        }
        catch (Exception ex) // General error handling block.
        {
            // Show an error notification.
            string errorMessage = "An unexpected error occurred: " + ex.Message;
            Notification.Show(Notification.DefaultDuration, Notification.DefaultTitle,
                errorMessage, ToolTipIcon.Error);

            return 1; // Exit code 1 indicates an error.
        }
    }
}
