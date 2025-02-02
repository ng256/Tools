/*
 * nmsg.exe - Simple messaging tool for displaying message box.
 * 
 * Usage: msgbox.exe [options]
 * 
 * Options:
 *  -title <title>      Sets the title of the message box.
 *  -message <message>  Sets the message of the message box.
 *  -buttons <buttons>  Sets the buttons of the message box".
 *  -icon <icon>        Sets the icon of the message box.
 *  -result             Sets the return code as dialog result.
 * 
 * Config file format (msgbox.ini):
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
using System.Windows.Forms;
using System.Ini;
using System.IO;
using System.Reflection; // The namespace for the IniFile class. For details,
                         // check out the GitHub page at https://github.com/ng256/IniFile.

[IniSection("Settings")]
class MsgBox
{
    /***** Default values. ********************************************************/

    // Help message, shown as the notification message if not overridden.
    internal const string HelpMessage =
        "Usage: msgbox.exe [options]\n" +
        "Displays a message box.\n" +
        "Options:\n" +
        "  -title <title> Sets the title of the message box\n" +
        "  -message <message> Sets the message of the message box\n" +
        "  -buttons <buttons> Sets the buttons of the message box\n" +
        "  -icon <icon> Sets the icon of the message box" +
        "  -result Sets the return code as dialog result";

    // Default notification title.
    internal const string DefaultTitle = "Message";

    /* Parameters that can be obtained via command-line argument or INI file. *****/

    // Icon type for the message box (e.g., Info, Warning, Error).
    public MessageBoxIcon Icon { get; set; } = MessageBoxIcon.None;

    // Buttons set for the message box (e.g., Ok, Cancel, etc).
    public MessageBoxButtons Buttons { get; set; } = MessageBoxButtons.OK;

    // Notification message text.
    public string Message { get; set; } = HelpMessage;

    // Notification title text.
    public string Title { get; set; } = DefaultTitle;

    /***** Shows the message box with the specified settings. **********************/

    public DialogResult Show()
    {
        return Show(Title, Message, Buttons, Icon);
    }

    public static DialogResult Show(string title,
        string message,
        MessageBoxButtons buttons,
        MessageBoxIcon icon)
    {
        return MessageBox.Show(message, title, buttons, icon);
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
            string exeFileName = Path.GetFileNameWithoutExtension(Assembly.GetExecutingAssembly().Location);
            string configFileName = Path.ChangeExtension(exeFileName, ".ini");

            // Load the configuration file.
            IniFile config = IniFile.LoadOrCreate(configFileName);

            // Read parameters from the "Settings" section.
            MsgBox msgBox = new MsgBox();
            config.ReadSettings(msgBox);
            bool setResult = config.ReadBoolean("settings", "result");

            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];

                // Process arguments prefixed with "-" or "/".
                if (arg.StartsWith("-") || arg.StartsWith("/"))
                {
                    bool notLast = i < args.Length - 1;

                    switch (arg.TrimStart('-', '/').ToLowerInvariant())
                    {
                        case "title" when notLast:
                            msgBox.Title = args[++i];
                            break;
                        case "message" when notLast:
                            msgBox.Message = args[++i];
                            break;
                        case "buttons" when notLast && Enum.TryParse(args[++i], true, out MessageBoxButtons buttons):
                            msgBox.Buttons = buttons;
                            break;
                        case "icon" when notLast && Enum.TryParse(args[++i], true, out MessageBoxIcon icon):
                            msgBox.Icon = icon;
                            break;
                        case "result":
                            setResult = true;
                            break;
                    }
                }
            }

            // Show the message box.
            int result = (int) msgBox.Show();

            return setResult ? result : 0; // Exit code for success.
        }
        catch (Exception ex) // General error handling block.
        {
            // Show an error notification.
            string errorMessage = "An unexpected error occurred: " + ex.Message;
            MsgBox.Show(errorMessage, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

            return -1; // Exit code that indicates an error.
        }
    }
}
