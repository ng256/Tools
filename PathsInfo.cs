using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace System.IO
{
	public static class PathsInfo
	{
		public enum ClassId
		{
			MyComputer,
			MyDocks,
			Recycle,
			Control,
			NetworkEnviroment,
			RemoteAccessNetwork,
			Journal,
			Fonts,
			PerfomanceTools,
			Gps,
			DefaultLocation,
			DllLibraries,
			PowerOptions,
			BiometricDevices,
			AdminTools,
			DisplaySettings,
			DevicesAndPrintres,
			FontSettings,
			HomegroupSettings,
			TaskbarSettings,
			FirewallSettings,
			AddRemoveSoftware,
			PrintersInfo,
			SyncManager,
			WirelessNetwork,
			ActionCenter,
			WindowsRestore,
			Personalization,
			Troubleshouting,
			DefaultPrograms,
			SpeechRecognition,
			RemoteDesctop,
			CredentialManager,
			WindowsUpdate,
			InstallSowtwareNet,
			LaunchMediaAndDevices,
			ParentalControl,
			BackupAndRestore,
			EaseAccessCenter,
			SystemSettings,
			NetworkSharingCenter,
			NetworkAdapterSettings,
			SystemTray,
			UserAccounts,
			GadgetSettings,
			BitLocker
		}

		public enum SPECIAL_FOLDERS
		{
			FLAG_CREATE = 0x8000,
			ADMINTOOLS = 48,
			ALTSTARTUP = 29,
			APPDATA = 26,
			BITBUCKET = 10,
			COMMON_ADMINTOOLS = 47,
			COMMON_APPDATA = 35,
			COMMON_ALTSTARTUP = 29,
			COMMON_DESKTOPDIRECTORY = 25,
			COMMON_DOCUMENTS = 46,
			COMMON_FAVORITES = 0x1F,
			COMMON_PROGRAMS = 23,
			COMMON_STARTMENU = 22,
			COMMON_STARTUP = 24,
			COMMON_TEMPLATES = 45,
			CONTROLS = 3,
			COOKIES = 33,
			DESKTOP = 0,
			DESKTOPDIRECTORY = 0x10,
			DRIVES = 17,
			FAVORITES = 6,
			FONTS = 5341,
			HISTORY = 34,
			INTERNET = 1,
			INTERNET_CACHE = 0x20,
			LOCAL_APPDATA = 28,
			MYPICTURES = 39,
			NETHOOD = 19,
			NETWORK = 18,
			PERSONAL = 5,
			PRINTERS = 4,
			PRINTHOOD = 27,
			PROFILE = 40,
			PROGRAM_FILES = 38,
			PROGRAM_FILES_COMMON = 43,
			PROGRAM_FILES_COMMONX86 = 44,
			PROGRAM_FILESX86 = 42,
			PROGRAMS = 2,
			RECENT = 8,
			SENDTO = 9,
			STARTMENU = 11,
			STARTUP = 7,
			SYSTEM = 37,
			SYSTEMX86 = 41,
			TEMPLATES = 21,
			WINDOWS = 36
		}

		public enum SHGFP
		{
			SHGFP_TYPE_CURRENT,
			SHGFP_TYPE_DEFAULT
		}

		internal const uint BUFF_SIZE = 4096u;

		internal const int MAX_PATH = 260;

		internal const int MAX_ENV_LENGTH = 32767;

		internal const string TEMP_PATH = "Temp";

		internal const string ALPHABET = "1234567890qwertyuiopasdfghjklzxcvbnm";

		internal const int RANDOM_LENGTH = 8;

		internal const int S_OK = 0;

		internal const int S_FALSE = 1;

		internal const int E_INVALIDARG = -2147024809;

		private static Random Rnd
		{
			get;
		} = new Random();


		public static string[] PathDirectories
		{
			get;
		} = GetPathDirectories();


		public static string TempPath
		{
			get;
		} = GetTempPath();


		public static string RootTempPath
		{
			get;
		} = GetRootTempPath();


		public static string AppDirectory
		{
			get;
		} = GetAppDirectory();


		public static string AppFullPath
		{
			get;
		} = GetAppFullPath();


		public static string AppFileName
		{
			get;
		} = GetAppFileName();


		[DllImport("kernel32.dll", BestFitMapping = false, CharSet = CharSet.Auto)]
		[return: MarshalAs(UnmanagedType.I4)]
		private static extern int GetEnvironmentVariable([In][MarshalAs(UnmanagedType.LPTStr)] string lpName, [Out][MarshalAs(UnmanagedType.LPTStr)] string lpBuffer, [In][MarshalAs(UnmanagedType.I4)] int nSize);

		[DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.I4)]
		private static extern int GetCurrentDirectory([In][MarshalAs(UnmanagedType.U4)] int nSize, [Out][MarshalAs(UnmanagedType.LPTStr)] string lpBuffer);

		[DllImport("kernel32.dll", BestFitMapping = false, CharSet = CharSet.Auto)]
		[return: MarshalAs(UnmanagedType.I4)]
		private static extern int GetTempPath([In][MarshalAs(UnmanagedType.I4)] int bufferLen, [Out][MarshalAs(UnmanagedType.LPTStr)] string lpBuffer);

		[DllImport("shell32.dll", BestFitMapping = false, CharSet = CharSet.Auto, EntryPoint = "SHGetFolderPath ")]
		[return: MarshalAs(UnmanagedType.I4)]
		private static extern int GetFolderPath([In][MarshalAs(UnmanagedType.I4)] IntPtr hwndOwner, [In][MarshalAs(UnmanagedType.I4)] SPECIAL_FOLDERS nIndex, [In][MarshalAs(UnmanagedType.I4)] IntPtr hToken, [In][MarshalAs(UnmanagedType.I4)] SHGFP dwFlags, [Out][MarshalAs(UnmanagedType.LPTStr)] string lpBuffer);

		[DllImport("kernel32.dll", CharSet = CharSet.Auto)]
		[return: MarshalAs(UnmanagedType.I4)]
		private static extern int GetModuleFileName([In] IntPtr hModule, [Out][MarshalAs(UnmanagedType.LPTStr)] string lpBuffer, [In][MarshalAs(UnmanagedType.I4)] int nSize);

		public static string GetEnvironmentVariable(string variable)
		{
			string buffer = new string('\0', 32767);
			int length = GetEnvironmentVariable(variable, buffer, 32767);
			if (length <= 0)
			{
				return string.Empty;
			}
			return buffer.Substring(0, length);
		}

		public static string[] GetPathDirectories()
		{
			return GetEnvironmentVariable("PATH").Split(';');
		}

		public static Guid GetClassGuid(ClassId id)
		{
			switch (id)
			{
			case ClassId.MyComputer:
				return new Guid("20D04FE0-3AEA-1069-A2D8-08002B30309D");
			case ClassId.MyDocks:
				return new Guid("450D8FBA-AD25-11D0-98A8-0800361B1103");
			case ClassId.Recycle:
				return new Guid("645FF040-5081-101B-9F08-00AA002F954E");
			case ClassId.Control:
				return new Guid("21EC2020-3AEA-1069-A2DD-08002B30309D");
			case ClassId.NetworkEnviroment:
				return new Guid("208D2C60-3AEA-1069-A2D7-08002B30309D");
			case ClassId.RemoteAccessNetwork:
				return new Guid("992CFFA0-F557-101A-88EC-00DD010CCC48");
			case ClassId.Journal:
				return new Guid("FF393560-C2A7-11CF-BFF4-444553540000");
			case ClassId.Fonts:
				return new Guid("BD84B380-8CA2-1069-AB1D-08000948F534");
			case ClassId.PerfomanceTools:
				return new Guid("78F3955E-3B90-4184-BD14-5397C15F1EFC");
			case ClassId.Gps:
				return new Guid("E9950154-C418-419e-A90A-20C5287AE24B");
			case ClassId.DefaultLocation:
				return new Guid("00C6D95F-329C-409a-81D7-C46C66EA7F33");
			case ClassId.DllLibraries:
				return new Guid("1D2680C9-0E2A-469d-B787-065558BC7D43");
			case ClassId.PowerOptions:
				return new Guid("025A5937-A6BE-4686-A844-36FE4BEC8B6D");
			case ClassId.BiometricDevices:
				return new Guid("0142e4d0-fb7a-11dc-ba4a-000ffe7ab428");
			case ClassId.AdminTools:
				return new Guid("D20EA4E1-3957-11d2-A40B-0C5020524153");
			case ClassId.DisplaySettings:
				return new Guid("C555438B-3C23-4769-A71F-B6D3D9B6053A");
			case ClassId.DevicesAndPrintres:
				return new Guid("A8A91A66-3A7D-4424-8D24-04E180695C7A");
			case ClassId.FontSettings:
				return new Guid("93412589-74D4-4E4E-AD0E-E0CB621440FD");
			case ClassId.HomegroupSettings:
				return new Guid("67CA7650-96E6-4FDD-BB43-A8E774F73A57");
			case ClassId.TaskbarSettings:
				return new Guid("05d7b0f4-2121-4eff-bf6b-ed3f69b894d9");
			case ClassId.FirewallSettings:
				return new Guid("4026492F-2F69-46B8-B9BF-5654FC07E423");
			case ClassId.AddRemoveSoftware:
				return new Guid("7b81be6a-ce2b-4676-a29e-eb907a5126c5");
			case ClassId.PrintersInfo:
				return new Guid("2227A280-3AEA-1069-A2DE-08002B30309D");
			case ClassId.SyncManager:
				return new Guid("9C73F5E5-7AE7-4E32-A8E8-8D23B85255BF");
			case ClassId.WirelessNetwork:
				return new Guid("1FA9085F-25A2-489B-85D4-86326EEDCD87");
			case ClassId.ActionCenter:
				return new Guid("BB64F8A7-BEE7-4E1A-AB8D-7D8273F7FDB6");
			case ClassId.WindowsRestore:
				return new Guid("9FE63AFD-59CF-4419-9775-ABCC3849F861");
			case ClassId.Personalization:
				return new Guid("ED834ED6-4B5A-4bfe-8F11-A626DCB6A921");
			case ClassId.Troubleshouting:
				return new Guid("C58C4893-3BE0-4B45-ABB5-A63E4B8C8651");
			case ClassId.DefaultPrograms:
				return new Guid("17cd9488-1228-4b2f-88ce-4298e93e0966");
			case ClassId.SpeechRecognition:
				return new Guid("58E3C745-D971-4081-9034-86E34B30836A");
			case ClassId.RemoteDesctop:
				return new Guid("241D7C96-F8BF-4F85-B01F-E2B043341A4B");
			case ClassId.CredentialManager:
				return new Guid("1206F5F1-0569-412C-8FEC-3204630DFB70");
			case ClassId.WindowsUpdate:
				return new Guid("36eef7db-88ad-4e81-ad49-0e313f0c35f8");
			case ClassId.InstallSowtwareNet:
				return new Guid("15eae92e-f17a-4431-9f28-805e482dafd4");
			case ClassId.LaunchMediaAndDevices:
				return new Guid("9C60DE1E-E5FC-40f4-A487-460851A8D915");
			case ClassId.ParentalControl:
				return new Guid("96AE8D84-A250-4520-95A5-A47A7E3C548B");
			case ClassId.BackupAndRestore:
				return new Guid("B98A2BEA-7D42-4558-8BD1-832F41BAC6FD");
			case ClassId.EaseAccessCenter:
				return new Guid("D555645E-D4F8-4c29-A827-D93C859C4F2A");
			case ClassId.SystemSettings:
				return new Guid("BB06C0E4-D293-4f75-8A90-CB05B6477EEE");
			case ClassId.NetworkSharingCenter:
				return new Guid("8E908FC9-BECC-40f6-915B-F4CA0E70D03D");
			case ClassId.NetworkAdapterSettings:
				return new Guid("7007ACC7-3202-11D1-AAD2-00805FC1270E");
			case ClassId.SystemTray:
				return new Guid("05d7b0f4-2121-4eff-bf6b-ed3f69b894d9");
			case ClassId.UserAccounts:
				return new Guid("60632754-c523-4b62-b45c-4172da012619");
			case ClassId.GadgetSettings:
				return new Guid("E95A4861-D57A-4be1-AD0F-35267E261739");
			case ClassId.BitLocker:
				return new Guid("D9EF8727-CAC2-4e60-809E-86F80A666C91");
			default:
				return Guid.Empty;
			}
		}

		public static string GetSpecialFolder(SPECIAL_FOLDERS folder, SHGFP shgfp = SHGFP.SHGFP_TYPE_CURRENT)
		{
			string buffer = new string('\0', 260);
			switch (GetFolderPath(IntPtr.Zero, folder, IntPtr.Zero, shgfp, buffer))
			{
			case 0:
			case 1:
				return buffer.Trim('0');
			case -2147024809:
				throw new ArgumentException(null, "folder");
			default:
				return string.Empty;
			}
		}

		public static string GetCurrentDirectory()
		{
			string buffer = new string('\0', 260);
			int length = GetCurrentDirectory(260, buffer);
			if (length <= 0)
			{
				return string.Empty;
			}
			return buffer.Substring(0, length);
		}

		public static string GetTempPath()
		{
			string buffer = new string('\0', 260);
			int length = GetTempPath(260, buffer);
			if (length <= 0)
			{
				return string.Empty;
			}
			return buffer.Substring(0, length);
		}

		public static string GetTempFileName(string prefix = "", int length = 8, string alphabet = "1234567890qwertyuiopasdfghjklzxcvbnm", string ext = ".tmp", bool fullPath = true)
		{
			if (length < 0)
			{
				length = 0;
			}
			if (string.IsNullOrEmpty(alphabet))
			{
				alphabet = "1234567890qwertyuiopasdfghjklzxcvbnm";
			}
			if (string.IsNullOrEmpty(ext))
			{
				ext = ".tmp";
			}
			if (ext[0] != '.')
			{
				ext = "." + ext;
			}
			int alphLength = alphabet.Length;
			int capacity = prefix.Length + ext.Length + length;
			if (fullPath)
			{
				capacity += TempPath.Length;
			}
			StringBuilder sb = new StringBuilder(prefix, capacity);
			for (int i = 0; i < length; i++)
			{
				int index = Rnd.Next(alphLength);
				sb.Append(alphabet[index]);
			}
			sb.Append(ext);
			string fileName = sb.ToString();
			string result = (fullPath ? Path.Combine(TempPath, fileName) : fileName);
			if (File.Exists(result))
			{
				length++;
				result = GetTempFileName(prefix, length, ext, alphabet, fullPath);
			}
			return result;
		}

		public static string GetRootTempPath()
		{
			string buffer = new string('\0', 260);
			int length = GetEnvironmentVariable("SystemDrive", buffer, 260);
			return Path.Combine(((length > 0) ? buffer.Substring(0, length) : "C:") + "\\", "Temp");
		}

		public static string GetAppDirectory()
		{
			string buffer = new string('\0', 260);
			int length = GetModuleFileName(IntPtr.Zero, buffer, 260);
			if (length <= 0)
			{
				return string.Empty;
			}
			return Path.GetDirectoryName(buffer.Substring(0, length));
		}

		public static string GetAppFullPath()
		{
			string buffer = new string('\0', 260);
			int length = GetModuleFileName(IntPtr.Zero, buffer, 260);
			if (length <= 0)
			{
				return string.Empty;
			}
			return Path.GetFullPath(buffer.Substring(0, length));
		}

		public static string GetAppFileName()
		{
			string buffer = new string('\0', 260);
			int length = GetModuleFileName(IntPtr.Zero, buffer, 260);
			if (length <= 0)
			{
				return string.Empty;
			}
			return Path.GetFileName(buffer.Substring(0, length));
		}

		public static string NormaliseDirectoryPath(string path)
		{
			if (path.Last() != Path.DirectorySeparatorChar)
			{
				return path + Path.DirectorySeparatorChar;
			}
			return path;
		}

		public static string GetRelativePath(string path, string basePath)
		{
			if (string.IsNullOrEmpty(basePath))
			{
				return path;
			}
			basePath = NormaliseDirectoryPath(basePath);
			return path.Replace(basePath, string.Empty);
		}

		public static string GetAltPath(string path)
		{
			return path.Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
		}

		public static string SearchFullPath(string fileName, bool system, params string[] customDirectories)
		{
			string result;
			if (system)
			{
				string[] pathDirectories = PathDirectories;
				for (int i = 0; i < pathDirectories.Length; i++)
				{
					result = Path.Combine(pathDirectories[i], fileName);
					if (File.Exists(result))
					{
						return result;
					}
				}
			}
			result = Path.Combine(GetCurrentDirectory(), fileName);
			if (File.Exists(result))
			{
				return result;
			}
			result = Path.Combine(AppDirectory, fileName);
			if (File.Exists(result))
			{
				return result;
			}
			if (customDirectories.Length != 0)
			{
				string[] pathDirectories = customDirectories;
				for (int i = 0; i < pathDirectories.Length; i++)
				{
					result = Path.Combine(pathDirectories[i], fileName);
					if (File.Exists(result))
					{
						return result;
					}
				}
			}
			return Path.GetFullPath(fileName);
		}

		public static Process ShowInExplorer(SPECIAL_FOLDERS folder)
		{
			string path = GetSpecialFolder(folder);
			if (string.IsNullOrEmpty(path))
			{
				throw new ArgumentOutOfRangeException("folder");
			}
			return Process.Start("explorer.exe /e," + path);
		}

		public static Process ShowInExplorer(Environment.SpecialFolder folder)
		{
			string path = Environment.GetFolderPath(folder, Environment.SpecialFolderOption.None);
			if (string.IsNullOrEmpty(path))
			{
				throw new ArgumentOutOfRangeException("folder");
			}
			return Process.Start("explorer.exe /e," + path);
		}

		public static Process ShowInExplorer(ClassId id)
		{
			Guid guid = GetClassGuid(id);
			if (guid == Guid.Empty)
			{
				throw new ArgumentOutOfRangeException("id");
			}
			return Process.Start($"::{{{guid}}}");
		}

		public static Process ShowInExplorer(string path)
		{
			if (path == null)
			{
				throw new ArgumentNullException("path");
			}
			if (path == string.Empty)
			{
				throw new ArgumentException(null, "path");
			}
			if (File.Exists(path))
			{
				return Process.Start("explorer.exe /e,/select," + path);
			}
			if (Directory.Exists(path))
			{
				return Process.Start("explorer.exe /e," + path);
			}
			throw new FileNotFoundException(null, path);
		}
	}
}
