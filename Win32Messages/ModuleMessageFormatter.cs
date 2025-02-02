using System.Globalization;
using System.Ini;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace System.ComponentModel
{
	[Serializable]
	[TypeConverter(typeof(ModuleMessageFormatterConverter))]
	internal class ModuleMessageFormatter : Win32MessageFormatter
	{
		private const string DEFAULT = "user32.dll";

		[NonSerialized]
		private string _moduleName = "user32.dll";

		[NonSerialized]
		private IntPtr _handle;

		[NonSerialized]
		private bool _disposed;

		[NonSerialized]
		private static readonly ModuleMessageFormatter _defaultFormatter = new ModuleMessageFormatter();

		private static readonly Encoding TextEncoding = Encoding.GetEncoding(CultureInfo.CurrentCulture.TextInfo.OEMCodePage);

		public string FileName => _moduleName;

		public new static ModuleMessageFormatter DefaultFormatter => _defaultFormatter;

		private ModuleMessageFormatter()
		{
		}

		public ModuleMessageFormatter(string moduleName)
		{
			_moduleName = moduleName;
			_handle = LoadLibrary(moduleName);
		}

		[DllImport("kernel32", CharSet = CharSet.Ansi, SetLastError = true)]
		private static extern IntPtr LoadLibrary([MarshalAs(UnmanagedType.LPStr)] string lpFileName);

		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
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
			StringBuilder lpBuffer = new StringBuilder(1024);
			try
			{
				handle = LoadLibrary(fileName);
				FreeLibrary(handle);
				int result;
				if (LoadString(handle, id, lpBuffer, 1024) == 0 && (result = GetLastError()) > 0)
				{
					throw new Win32Exception(result);
				}
			}
			finally
			{
				if (handle != IntPtr.Zero)
				{
					FreeLibrary(handle);
				}
			}
			return lpBuffer.ToString();
		}

		internal string LoadString(uint id)
		{
			if (_disposed)
			{
				throw new ObjectDisposedException(_moduleName);
			}
			StringBuilder lpBuffer = new StringBuilder(1024);
			int result;
			if (LoadString(_handle, id, lpBuffer, 1024) == 0 && (result = GetLastError()) > 0)
			{
				throw new Win32Exception(result);
			}
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

		[Obsolete]
		public override string FormatMessage(uint messageId, Win32FormatMessageFlags flags, string source, params string[] arguments)
		{
			return FormatMessage(messageId, arguments);
		}

		[Obsolete]
		public override string FormatMessage(uint messageId, Win32FormatMessageFlags flags, string source, bool ignoreNewLine, params string[] arguments)
		{
			return FormatMessage(messageId, ignoreNewLine, arguments);
		}

		public Exception GetException(uint messageId, params string[] parameters)
		{
			return new Exception(FormatMessage(messageId, parameters));
		}

		public Exception GetException(uint messageId, bool ignoreNewLine, params string[] parameters)
		{
			return new Exception(FormatMessage(messageId, parameters));
		}

		public override void ReadSettings(Assembly assembly = null)
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
					ModuleStringAttribute moduleString;
					if (!property.CanWrite || (moduleString = property.GetCustomAttributes(typeof(ModuleStringAttribute), false).FirstOrDefault() as ModuleStringAttribute) == null || moduleString.Name != _moduleName)
					{
						continue;
					}
					uint msgId = moduleString.MessageId;
					string[] args = moduleString.Arguments;
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
		public override void WriteSettings(Assembly assembly = null)
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
