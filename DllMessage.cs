using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace System.ComponentModel
{
    public class ModuleMessageFormatter : Win32MessageFormatter
    {
        private const string DEFAULT = "user32.dll";
        private string _moduleName = DEFAULT;
        private IntPtr _handle;
        private bool _disposed = false;
        private static readonly ModuleMessageFormatter _defaultFormatter = new ModuleMessageFormatter();

        public string FileName => _moduleName;

        private ModuleMessageFormatter() : base()
        {
        }

        public ModuleMessageFormatter(string moduleName) : base()
        {
            _moduleName = moduleName;
            _handle = LoadLibrary(moduleName);

        }

        public new static ModuleMessageFormatter DefaultFormatter => _defaultFormatter;

        private static readonly Encoding TextEncoding =
            Encoding.GetEncoding(CultureInfo.CurrentCulture.TextInfo.OEMCodePage);

        [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Ansi)]
        private static extern IntPtr LoadLibrary([MarshalAs(UnmanagedType.LPStr)] string lpFileName);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern int LoadString(IntPtr hInstance, uint uID, [Out] StringBuilder lpBuffer, int nBufferMax);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern int GetLastError();

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool FreeLibrary(IntPtr hInstance);

        public override void Dispose()
        {
            if (!_disposed && _handle != IntPtr.Zero)
            {
                FreeLibrary(_handle);
                _disposed = true;
            }
            base.Dispose();
            GC.SuppressFinalize(this);
        }

        internal static string LoadString(uint id, string fileName)
        {
            IntPtr handle = IntPtr.Zero;
            StringBuilder lpBuffer = new StringBuilder(BUFF_SIZE);
            try
            {
                handle = LoadLibrary(fileName);
                int result = LoadString(handle, id, lpBuffer, BUFF_SIZE);
                if (result == 0 && (result = GetLastError()) > 0)
                    throw new Win32Exception(result);
            }
            finally
            {
                if (handle != IntPtr.Zero)
                    FreeLibrary(handle);
            }

            return lpBuffer.ToString();
        }

        internal string LoadString(uint id)
        {
            if (_disposed)
                throw new ObjectDisposedException(_moduleName);
            StringBuilder lpBuffer = new StringBuilder(BUFF_SIZE);
            int result = LoadString(_handle, id, lpBuffer, BUFF_SIZE);
            if (result == 0 && (result = GetLastError()) > 0)
                throw new Win32Exception(result);

            return lpBuffer.ToString();
        }

        public override string ToString()
        {
            return _moduleName;
        }

        public override string FormatMessage(string moduleName, uint messageId, params string[] arguments)
        {
            string messgae = LoadString(messageId, moduleName);
            return base.FormatMessage(messgae, arguments);
        }

        public override string FormatMessage(string moduleName, uint messageId, bool ignoreNewLine, params string[] arguments)
        {
            string messgae = LoadString(messageId, moduleName);
            return base.FormatMessage(messgae, ignoreNewLine, arguments);
        }

        public override string FormatMessage(uint messageId, params string[] arguments)
        {
            string messgae = LoadString(messageId);
            return base.FormatMessage(messgae, arguments);
        }

        public override string FormatMessage(uint messageId, bool ignoreNewLine, params string[] arguments)
        {
            string messgae = LoadString(messageId);
            return base.FormatMessage(messgae, ignoreNewLine, arguments);
        }

        public override string FormatMessage(uint messageId)
        {
            string messgae = LoadString(messageId);
            return base.FormatMessage(messgae);
        }

        public override string FormatMessage(uint messageId, bool ignoreNewLine)
        {
            string messgae = LoadString(messageId);
            return base.FormatMessage(messgae, ignoreNewLine);
        }

    }

    public class Win32MessageFormatter : IDisposable
    {
        protected const int BUFF_SIZE = 1024;
        private static readonly int[] _installedLangs;
        private static readonly Win32MessageFormatter _defaultFormatter = new Win32MessageFormatter();
        private readonly int _lcid;

        public virtual void Dispose()
        {
        }

        public int LCID => _lcid;

        public static Win32MessageFormatter DefaultFormatter => _defaultFormatter;

        static Win32MessageFormatter()
        {
            CultureInfo[] installedCultures = CultureInfo.GetCultures(CultureTypes.InstalledWin32Cultures);
            CultureInfo[] specificCultures = CultureInfo.GetCultures(CultureTypes.SpecificCultures);
            List<int> installedLangs = new List<int>(installedCultures.Length);

            foreach (CultureInfo culture in specificCultures)
            {
                if (installedCultures.Contains(culture))
                    installedLangs.Add(culture.LCID);
            }

            _installedLangs = installedLangs.ToArray();
        }

        public Win32MessageFormatter()
        {
            _lcid = CultureInfo.CurrentCulture.LCID;
        }

        public Win32MessageFormatter(int lcid) : this()
        {
            if (_installedLangs.Contains(lcid))
                _lcid = lcid;
        }

        public Win32MessageFormatter(CultureInfo culture) : this()
        {
            if (culture != null && _installedLangs.Contains(culture.LCID))
                _lcid = culture.LCID;
        }

        public Win32MessageFormatter(TextInfo info) : this()
        {
            if (info != null && _installedLangs.Contains(info.LCID))
                _lcid = info.LCID;
        }

        public Win32MessageFormatter(string name)
        {

            try
            {
                CultureInfo culture = CultureInfo.GetCultureInfo(name);
                if (_installedLangs.Contains(culture.LCID))
                    _lcid = culture.LCID;
            }
            catch
            {
                _lcid = CultureInfo.CurrentCulture.LCID;
            }
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetModuleHandle([MarshalAs(UnmanagedType.LPStr)] string lpModuleName);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.I4)]
        private static extern int FormatMessage(
            int dwFlags,
            IntPtr lpSource,
            uint dwMessageId,
            int dwLanguageId,
            [Out] StringBuilder lpBuffer,
            int nSize,
            string[] arguments);

        public virtual string FormatMessage(uint messageId, Win32FormatMessageFlags flags, string source,
            params string[] arguments)
        {
            if (string.IsNullOrEmpty(source))
            {
                flags &= ~Win32FormatMessageFlags.FORMAT_MESSAGE_FROM_STRING;
                flags &= ~Win32FormatMessageFlags.FORMAT_MESSAGE_FROM_HMODULE;
            }

            if (arguments.Length == 0)
                flags &= ~Win32FormatMessageFlags.FORMAT_MESSAGE_ARGUMENT_ARRAY;

            StringBuilder buffer = new StringBuilder(BUFF_SIZE);
            IntPtr formatPtr = string.IsNullOrEmpty(source) ? IntPtr.Zero : Marshal.StringToHGlobalAnsi(source);
            int length = FormatMessage((int)flags, formatPtr, messageId, _lcid, buffer, BUFF_SIZE, arguments);
            string message = length > 0 ? buffer.ToString(0, length) : string.Empty;
            return message;
        }

        public virtual string FormatMessage(uint messageId, Win32FormatMessageFlags flags,
            string source, bool ignoreNewLine, params string[] arguments)
        {
            if (ignoreNewLine)
                flags |= Win32FormatMessageFlags.FORMAT_MESSAGE_MAX_WIDTH_MASK;
            else
                flags &= ~Win32FormatMessageFlags.FORMAT_MESSAGE_MAX_WIDTH_MASK;

            if (string.IsNullOrEmpty(source))
            {
                flags &= ~Win32FormatMessageFlags.FORMAT_MESSAGE_FROM_STRING;
                flags &= ~Win32FormatMessageFlags.FORMAT_MESSAGE_FROM_HMODULE;
            }

            if (arguments.Length == 0)
                flags &= ~Win32FormatMessageFlags.FORMAT_MESSAGE_ARGUMENT_ARRAY;

            StringBuilder buffer = new StringBuilder(BUFF_SIZE);
            IntPtr formatPtr = string.IsNullOrEmpty(source) ? IntPtr.Zero : Marshal.StringToHGlobalAnsi(source);
            int length = FormatMessage((int)flags, formatPtr, messageId, _lcid, buffer, BUFF_SIZE, arguments);
            string message = length > 0 ? buffer.ToString(0, length) : string.Empty;
            return message;
        }

        public virtual string FormatMessage(uint messageId, params string[] arguments)
        {
            StringBuilder buffer = new StringBuilder(BUFF_SIZE);

            Win32FormatMessageFlags flags = Win32FormatMessageFlags.FORMAT_MESSAGE_FROM_SYSTEM;
            flags |= arguments.Length > 0
                ? Win32FormatMessageFlags.FORMAT_MESSAGE_ARGUMENT_ARRAY
                : Win32FormatMessageFlags.FORMAT_MESSAGE_IGNORE_INSERTS;

            int length = FormatMessage((int)flags, IntPtr.Zero, messageId, _lcid, buffer, BUFF_SIZE, arguments);
            string message = length > 0 ? buffer.ToString(0, length) : string.Empty;
            return message;
        }

        public virtual string FormatMessage(uint messageId, bool ignoreNewLine, params string[] arguments)
        {
            StringBuilder buffer = new StringBuilder(BUFF_SIZE);

            Win32FormatMessageFlags flags = Win32FormatMessageFlags.FORMAT_MESSAGE_FROM_SYSTEM;
            flags |= arguments.Length > 0
                ? Win32FormatMessageFlags.FORMAT_MESSAGE_ARGUMENT_ARRAY
                : Win32FormatMessageFlags.FORMAT_MESSAGE_IGNORE_INSERTS;

            if (ignoreNewLine)
                flags |= Win32FormatMessageFlags.FORMAT_MESSAGE_MAX_WIDTH_MASK;

            int length = FormatMessage((int)flags, IntPtr.Zero, messageId, _lcid, buffer, BUFF_SIZE, arguments);
            string message = length > 0 ? buffer.ToString(0, length) : string.Empty;
            return message;
        }

        public virtual string FormatMessage(uint messageId)
        {
            StringBuilder buffer = new StringBuilder(BUFF_SIZE);

            Win32FormatMessageFlags flags = Win32FormatMessageFlags.FORMAT_MESSAGE_FROM_SYSTEM |
                                            Win32FormatMessageFlags.FORMAT_MESSAGE_IGNORE_INSERTS;

            int length = FormatMessage((int)flags, IntPtr.Zero, messageId, _lcid, buffer, BUFF_SIZE, null);
            string message = length > 0 ? buffer.ToString(0, length) : string.Empty;
            return message;
        }

        public virtual string FormatMessage(uint messageId, bool ignoreNewLine)
        {
            StringBuilder buffer = new StringBuilder(BUFF_SIZE);

            Win32FormatMessageFlags flags = Win32FormatMessageFlags.FORMAT_MESSAGE_FROM_SYSTEM |
                                            Win32FormatMessageFlags.FORMAT_MESSAGE_IGNORE_INSERTS;

            if (ignoreNewLine)
                flags |= Win32FormatMessageFlags.FORMAT_MESSAGE_MAX_WIDTH_MASK;

            int length = FormatMessage((int)flags, IntPtr.Zero, messageId, _lcid, buffer, BUFF_SIZE, null);
            string message = length > 0 ? buffer.ToString(0, length) : string.Empty;
            return message;
        }

        public virtual string FormatMessage(string moduleName, uint messageId, params string[] arguments)
        {
            StringBuilder buffer = new StringBuilder(BUFF_SIZE);
            IntPtr modulePtr = string.IsNullOrEmpty(moduleName)
                ? IntPtr.Zero
                : GetModuleHandle(moduleName);

            Win32FormatMessageFlags flags = Win32FormatMessageFlags.FORMAT_MESSAGE_FROM_HMODULE;
            flags |= arguments.Length > 0
                ? Win32FormatMessageFlags.FORMAT_MESSAGE_ARGUMENT_ARRAY
                : Win32FormatMessageFlags.FORMAT_MESSAGE_IGNORE_INSERTS;

            int length = FormatMessage((int)flags, modulePtr, messageId, _lcid, buffer, BUFF_SIZE, arguments);
            string message = length > 0 ? buffer.ToString(0, length) : string.Empty;
            return message;
        }

        public virtual string FormatMessage(string moduleName, uint messageId, bool ignoreNewLine,
            params string[] arguments)
        {
            StringBuilder buffer = new StringBuilder(BUFF_SIZE);
            IntPtr modulePtr = string.IsNullOrEmpty(moduleName)
                ? IntPtr.Zero
                : GetModuleHandle(moduleName);

            Win32FormatMessageFlags flags = Win32FormatMessageFlags.FORMAT_MESSAGE_FROM_HMODULE;
            flags |= arguments.Length > 0
                ? Win32FormatMessageFlags.FORMAT_MESSAGE_ARGUMENT_ARRAY
                : Win32FormatMessageFlags.FORMAT_MESSAGE_IGNORE_INSERTS;

            if (ignoreNewLine)
                flags |= Win32FormatMessageFlags.FORMAT_MESSAGE_MAX_WIDTH_MASK;

            int length = FormatMessage((int)flags, modulePtr, messageId, _lcid, buffer, BUFF_SIZE, arguments);
            string message = length > 0 ? buffer.ToString(0, length) : string.Empty;
            return message;
        }

        public virtual string FormatMessage(string message, params string[] arguments)
        {
            StringBuilder buffer = new StringBuilder(BUFF_SIZE);
            IntPtr formatPtr = string.IsNullOrEmpty(message)
                ? IntPtr.Zero
                : Marshal.StringToHGlobalAnsi(message);

            Win32FormatMessageFlags flags = Win32FormatMessageFlags.FORMAT_MESSAGE_FROM_STRING;
            flags |= arguments.Length > 0
                ? Win32FormatMessageFlags.FORMAT_MESSAGE_ARGUMENT_ARRAY
                : Win32FormatMessageFlags.FORMAT_MESSAGE_IGNORE_INSERTS;

            int length = FormatMessage((int)flags, formatPtr, 0, _lcid, buffer, BUFF_SIZE, arguments);
            message = buffer.ToString(0, length);
            return message;
        }

        public virtual string FormatMessage(string message, bool ignoreNewLine, params string[] arguments)
        {
            StringBuilder buffer = new StringBuilder(BUFF_SIZE);
            IntPtr formatPtr = string.IsNullOrEmpty(message)
                ? IntPtr.Zero
                : Marshal.StringToHGlobalAnsi(message);

            Win32FormatMessageFlags flags = Win32FormatMessageFlags.FORMAT_MESSAGE_FROM_STRING;
            flags |= arguments.Length > 0
                ? Win32FormatMessageFlags.FORMAT_MESSAGE_ARGUMENT_ARRAY
                : Win32FormatMessageFlags.FORMAT_MESSAGE_IGNORE_INSERTS;

            if (ignoreNewLine)
                flags |= Win32FormatMessageFlags.FORMAT_MESSAGE_MAX_WIDTH_MASK;

            int length = FormatMessage((int)flags, formatPtr, 0, _lcid, buffer, BUFF_SIZE, arguments);
            message = buffer.ToString(0, length);
            return message;
        }

        public Win32Exception CreateWin32Exception(uint msgCode, params string[] parameters)
        {
            string message = FormatMessage(msgCode, parameters);
            return new Win32Exception((int)msgCode, message);
        }
    }

    [Flags]
    public enum Win32FormatMessageFlags : int
    {
        FORMAT_MESSAGE_NONE = 0,
        FORMAT_MESSAGE_ALLOCATE_BUFFER = 0x00000100,
        FORMAT_MESSAGE_ARGUMENT_ARRAY = 0x00002000,
        FORMAT_MESSAGE_FROM_HMODULE = 0x00000800,
        FORMAT_MESSAGE_FROM_STRING = 0x00000400,
        FORMAT_MESSAGE_FROM_SYSTEM = 0x00001000,
        FORMAT_MESSAGE_IGNORE_INSERTS = 0x00000200,
        FORMAT_MESSAGE_MAX_WIDTH_MASK = 0x000000FF
    }
}
