using System;
using System.IO;
using System.Runtime.InteropServices;

static class Program
{
    [DllImport("kernel32.dll", SetLastError = true)]
    static extern IntPtr LoadLibrary(string lpLibFileName);

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern void FreeLibrary(IntPtr module);

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    static extern uint GetModuleFileName(IntPtr hModule, System.Text.StringBuilder lpFilename, uint nSize);

    delegate void MessageBox(IntPtr wnd, string title, string message, uint type);

    const int MAX_PATH = 260;
    const string MODULE_NAME = "MSGBOX";

    static string GetModuleFileName()
    {
        System.Text.StringBuilder fileName = new System.Text.StringBuilder(MAX_PATH);
        uint count = GetModuleFileName(IntPtr.Zero, fileName, (uint)fileName.Capacity);

        return count > 0
            ? Path.GetFileNameWithoutExtension(fileName.ToString()).ToUpper()
            : MODULE_NAME;
    }

    static int Main(string[] args)
    {
        if (args.Length < 2 || args.Length == 1 && args[0] == "/?")
        {
            string moduleName = GetModuleFileName();
            Console.WriteLine($"Usage: {moduleName} \"Title\" \"Message\"");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("Title         The dialog box title.");
            Console.WriteLine("Message         The message to be displayed.");
            return 1;
        }

        string title = args[0];
        string message = args[1];

        try
        {
            IntPtr hUser32 = LoadLibrary("user32.dll");
            if (hUser32 == IntPtr.Zero)
                throw new Exception("Error loading user32.dll");

            try
            {
                IntPtr pMessageBoxA = GetProcAddress(hUser32, "MessageBoxA");
                if (pMessageBoxA == IntPtr.Zero)
                    throw new Exception("Error getting address of MessageBox");

                MessageBox msgBoxShow = (MessageBox)Marshal.GetDelegateForFunctionPointer(pMessageBoxA, typeof(MessageBox));
                msgBoxShow(IntPtr.Zero, title, message, 0);
            }
            finally
            {
                FreeLibrary(hUser32);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return 1;
        }

        return 0;
    }
}
