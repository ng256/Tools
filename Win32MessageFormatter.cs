using System.Collections.Generic;
using System.Globalization;
using System.Ini;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace System.ComponentModel
{
	[Serializable]
	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	[TypeConverter(typeof(Win32MessageFormatterConverter))]
	internal class Win32MessageFormatter : IInitializer, IDisposable
	{
		protected const int BUFF_SIZE = 1024;

		[NonSerialized]
		private static readonly int[] _installedLangs;

		[NonSerialized]
		private static readonly Win32MessageFormatter _defaultFormatter;

		[MarshalAs(UnmanagedType.I4)]
		private readonly int _lcid;

		public int LCID => _lcid;

		public static Win32MessageFormatter DefaultFormatter => _defaultFormatter;

		public virtual void Dispose()
		{
		}

		static Win32MessageFormatter()
		{
			_defaultFormatter = new Win32MessageFormatter();
			CultureInfo[] installedCultures = CultureInfo.GetCultures(CultureTypes.InstalledWin32Cultures);
			CultureInfo[] specificCultures = CultureInfo.GetCultures(CultureTypes.SpecificCultures);
			List<int> installedLangs = new List<int>(installedCultures.Length);
			CultureInfo[] array = specificCultures;
			foreach (CultureInfo culture in array)
			{
				if (installedCultures.Contains(culture))
				{
					installedLangs.Add(culture.LCID);
				}
			}
			_installedLangs = installedLangs.ToArray();
		}

		public Win32MessageFormatter()
		{
			_lcid = CultureInfo.CurrentCulture.LCID;
		}

		public Win32MessageFormatter(int lcid)
			: this()
		{
			if (_installedLangs.Contains(lcid))
			{
				_lcid = lcid;
			}
		}

		public Win32MessageFormatter(CultureInfo culture)
			: this()
		{
			if (culture != null && _installedLangs.Contains(culture.LCID))
			{
				_lcid = culture.LCID;
			}
		}

		public Win32MessageFormatter(TextInfo info)
			: this()
		{
			if (info != null && _installedLangs.Contains(info.LCID))
			{
				_lcid = info.LCID;
			}
		}

		public Win32MessageFormatter(string name)
		{
			try
			{
				CultureInfo culture = CultureInfo.GetCultureInfo(name);
				if (_installedLangs.Contains(culture.LCID))
				{
					_lcid = culture.LCID;
				}
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
		private static extern int FormatMessage(int dwFlags, IntPtr lpSource, uint dwMessageId, int dwLanguageId, [Out] StringBuilder lpBuffer, int nSize, string[] arguments);

		public virtual string FormatMessage(uint messageId, Win32FormatMessageFlags flags, string source, params string[] arguments)
		{
			if (string.IsNullOrEmpty(source))
			{
				flags &= ~Win32FormatMessageFlags.FORMAT_MESSAGE_FROM_STRING;
				flags &= ~Win32FormatMessageFlags.FORMAT_MESSAGE_FROM_HMODULE;
			}
			if (arguments.Length == 0)
			{
				flags &= ~Win32FormatMessageFlags.FORMAT_MESSAGE_ARGUMENT_ARRAY;
			}
			StringBuilder buffer = new StringBuilder(1024);
			IntPtr formatPtr = (string.IsNullOrEmpty(source) ? IntPtr.Zero : Marshal.StringToHGlobalAnsi(source));
			int length = FormatMessage((int)flags, formatPtr, messageId, _lcid, buffer, 1024, arguments);
			if (length <= 0)
			{
				return string.Empty;
			}
			return buffer.ToString(0, length);
		}

		public virtual string FormatMessage(uint messageId, Win32FormatMessageFlags flags, string source, bool ignoreNewLine, params string[] arguments)
		{
			flags = ((!ignoreNewLine) ? (flags & ~Win32FormatMessageFlags.FORMAT_MESSAGE_MAX_WIDTH_MASK) : (flags | Win32FormatMessageFlags.FORMAT_MESSAGE_MAX_WIDTH_MASK));
			if (string.IsNullOrEmpty(source))
			{
				flags &= ~Win32FormatMessageFlags.FORMAT_MESSAGE_FROM_STRING;
				flags &= ~Win32FormatMessageFlags.FORMAT_MESSAGE_FROM_HMODULE;
			}
			if (arguments.Length == 0)
			{
				flags &= ~Win32FormatMessageFlags.FORMAT_MESSAGE_ARGUMENT_ARRAY;
			}
			StringBuilder buffer = new StringBuilder(1024);
			IntPtr formatPtr = (string.IsNullOrEmpty(source) ? IntPtr.Zero : Marshal.StringToHGlobalAnsi(source));
			int length = FormatMessage((int)flags, formatPtr, messageId, _lcid, buffer, 1024, arguments);
			if (length <= 0)
			{
				return string.Empty;
			}
			return buffer.ToString(0, length);
		}

		public virtual string FormatMessage(uint messageId, params string[] arguments)
		{
			StringBuilder buffer = new StringBuilder(1024);
			int length = FormatMessage(0x1000 | ((arguments.Length != 0) ? 8192 : 512), IntPtr.Zero, messageId, _lcid, buffer, 1024, arguments);
			if (length <= 0)
			{
				return string.Empty;
			}
			return buffer.ToString(0, length);
		}

		public virtual string FormatMessage(uint messageId, bool ignoreNewLine, params string[] arguments)
		{
			StringBuilder buffer = new StringBuilder(1024);
			Win32FormatMessageFlags flags = Win32FormatMessageFlags.FORMAT_MESSAGE_FROM_SYSTEM;
			flags |= ((arguments.Length != 0) ? Win32FormatMessageFlags.FORMAT_MESSAGE_ARGUMENT_ARRAY : Win32FormatMessageFlags.FORMAT_MESSAGE_IGNORE_INSERTS);
			if (ignoreNewLine)
			{
				flags |= Win32FormatMessageFlags.FORMAT_MESSAGE_MAX_WIDTH_MASK;
			}
			int length = FormatMessage((int)flags, IntPtr.Zero, messageId, _lcid, buffer, 1024, arguments);
			if (length <= 0)
			{
				return string.Empty;
			}
			return buffer.ToString(0, length);
		}

		public virtual string FormatMessage(uint messageId)
		{
			StringBuilder buffer = new StringBuilder(1024);
			int length = FormatMessage(4608, IntPtr.Zero, messageId, _lcid, buffer, 1024, null);
			if (length <= 0)
			{
				return string.Empty;
			}
			return buffer.ToString(0, length);
		}

		public virtual string FormatMessage(uint messageId, bool ignoreNewLine)
		{
			StringBuilder buffer = new StringBuilder(1024);
			Win32FormatMessageFlags flags = Win32FormatMessageFlags.FORMAT_MESSAGE_FROM_SYSTEM | Win32FormatMessageFlags.FORMAT_MESSAGE_IGNORE_INSERTS;
			if (ignoreNewLine)
			{
				flags |= Win32FormatMessageFlags.FORMAT_MESSAGE_MAX_WIDTH_MASK;
			}
			int length = FormatMessage((int)flags, IntPtr.Zero, messageId, _lcid, buffer, 1024, null);
			if (length <= 0)
			{
				return string.Empty;
			}
			return buffer.ToString(0, length);
		}

		public virtual string FormatMessage(string moduleName, uint messageId, params string[] arguments)
		{
			StringBuilder buffer = new StringBuilder(1024);
			IntPtr modulePtr = (string.IsNullOrEmpty(moduleName) ? IntPtr.Zero : GetModuleHandle(moduleName));
			int length = FormatMessage(0x800 | ((arguments.Length != 0) ? 8192 : 512), modulePtr, messageId, _lcid, buffer, 1024, arguments);
			if (length <= 0)
			{
				return string.Empty;
			}
			return buffer.ToString(0, length);
		}

		public virtual string FormatMessage(string moduleName, uint messageId, bool ignoreNewLine, params string[] arguments)
		{
			StringBuilder buffer = new StringBuilder(1024);
			IntPtr modulePtr = (string.IsNullOrEmpty(moduleName) ? IntPtr.Zero : GetModuleHandle(moduleName));
			Win32FormatMessageFlags flags = Win32FormatMessageFlags.FORMAT_MESSAGE_FROM_HMODULE;
			flags |= ((arguments.Length != 0) ? Win32FormatMessageFlags.FORMAT_MESSAGE_ARGUMENT_ARRAY : Win32FormatMessageFlags.FORMAT_MESSAGE_IGNORE_INSERTS);
			if (ignoreNewLine)
			{
				flags |= Win32FormatMessageFlags.FORMAT_MESSAGE_MAX_WIDTH_MASK;
			}
			int length = FormatMessage((int)flags, modulePtr, messageId, _lcid, buffer, 1024, arguments);
			if (length <= 0)
			{
				return string.Empty;
			}
			return buffer.ToString(0, length);
		}

		public virtual string FormatMessage(string message, params string[] arguments)
		{
			StringBuilder buffer = new StringBuilder(1024);
			IntPtr formatPtr = (string.IsNullOrEmpty(message) ? IntPtr.Zero : Marshal.StringToHGlobalAnsi(message));
			int length = FormatMessage(0x400 | ((arguments.Length != 0) ? 8192 : 512), formatPtr, 0u, _lcid, buffer, 1024, arguments);
			message = buffer.ToString(0, length);
			return message;
		}

		public virtual string FormatMessage(string message, bool ignoreNewLine, params string[] arguments)
		{
			StringBuilder buffer = new StringBuilder(1024);
			IntPtr formatPtr = (string.IsNullOrEmpty(message) ? IntPtr.Zero : Marshal.StringToHGlobalAnsi(message));
			Win32FormatMessageFlags flags = Win32FormatMessageFlags.FORMAT_MESSAGE_FROM_STRING;
			flags |= ((arguments.Length != 0) ? Win32FormatMessageFlags.FORMAT_MESSAGE_ARGUMENT_ARRAY : Win32FormatMessageFlags.FORMAT_MESSAGE_IGNORE_INSERTS);
			if (ignoreNewLine)
			{
				flags |= Win32FormatMessageFlags.FORMAT_MESSAGE_MAX_WIDTH_MASK;
			}
			int length = FormatMessage((int)flags, formatPtr, 0u, _lcid, buffer, 1024, arguments);
			message = buffer.ToString(0, length);
			return message;
		}

		public Win32Exception GetWin32Exception(uint msgCode, params string[] parameters)
		{
			string message = FormatMessage(msgCode, parameters);
			return new Win32Exception((int)msgCode, message);
		}

		public override string ToString()
		{
			string name = CultureInfo.GetCultureInfo(_lcid).DisplayName;
			if (!string.IsNullOrEmpty(name))
			{
				return name;
			}
			return _lcid.ToString();
		}

		public virtual void ReadSettings(Assembly assembly = null)
		{
			if (assembly == null)
			{
				assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
			}
			Type[] types = assembly.GetTypes();
			for (int i = 0; i < types.Length; i++)
			{
				PropertyInfo[] properties = types[i].GetProperties(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
				foreach (PropertyInfo property in properties)
				{
					Win32ErrorMessageAttribute win32Message;
					if (!property.CanWrite || (win32Message = property.GetCustomAttributes(typeof(Win32ErrorMessageAttribute), false).FirstOrDefault() as Win32ErrorMessageAttribute) == null)
					{
						continue;
					}
					uint msgId = win32Message.MessageId;
					string[] args = win32Message.Arguments;
					try
					{
						if (property.PropertyType == typeof(string))
						{
							object newPropertyValue = FormatMessage(msgId, args);
							property.SetPropertyValue(newPropertyValue);
						}
					}
					catch (Exception)
					{
					}
				}
			}
		}

		[Obsolete]
		public virtual void WriteSettings(Assembly assembly = null)
		{
			try
			{
				throw new NotImplementedException();
			}
			catch (Exception)
			{
			}
		}
	}
}
