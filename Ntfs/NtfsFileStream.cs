/*******************************************************************************************************

    File: NtfsFileStream.cs
    Class: System.IO.NtfsFileStream
    Description: The NtfsFileStream class provides methods for extracting alternate file streams -
    metadata associated with an NTFS file system object. Essentially, file streams represent
    the combination of multiple files into one group with a single common file name (each stream has its own
    additional name).

    * Based on the Trinet.Core.IO.Ntfs project Copyright (C) 2002-2010 Richard Deeming
    * This code is free software: you can redistribute it and/or modify it under the terms of either
    * - the Code Project Open License (CPOL) version 1 or later;
    * - the GNU General Public License as published by the Free Software Foundation, version 3 or later;
    * - the BSD 2-Clause License;
    *
    * This code is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY;
    * without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
    * See the license files for details.
    *
    * You should have received a copy of the licenses along with this code.
    * If not, see <http://www.codeproject.com/info/cpol10.aspx>, <http://www.gnu.org/licenses/>
    * and <http://opensource.org/licenses/bsd-license.php>.

    © NG256 2019

********************************************************************************************************/

using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.AccessControl;
using System.Security.Permissions;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace System.IO
{
    #region Additional info

    /// <summary>
    /// Represents the properties of an NTFS stream.
    /// </summary>
    [Serializable]
    public class NtfsInfo : IEquatable<NtfsInfo>
    {
        /// <summary>
        /// Gets or sets the attributes of the NTFS file stream.
        /// </summary>
        public NtfsType StreamType { get; set; }

        /// <summary>
        /// Gets or sets the type of data in the NTFS stream.
        /// </summary>
        public NtfsAttributes StreamAttributes { get; set; }

        /// <summary>
        /// Gets or sets the size of the stream in bytes.
        /// </summary>
        public long Size { get; set; }

        /// <summary>
        /// Gets or sets the name of the stream.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Checks if two specified instances of <see cref="NtfsInfo"/> are equal.
        /// </summary>
        /// <param name="left">The first object to compare.</param>
        /// <param name="right">The second object to compare.</param>
        /// <returns><see langword="true"/> if the specified objects are equal.</returns>
        public static bool operator ==(NtfsInfo left, NtfsInfo right)
        {
            return left?.Equals(right) ?? false;
        }

        /// <summary>
        /// Checks if two specified instances of <see cref="NtfsInfo"/> are different.
        /// </summary>
        /// <param name="left">The first object to compare.</param>
        /// <param name="right">The second object to compare.</param>
        /// <returns><see langword="true"/> if the specified objects are equal.</returns>
        public static bool operator !=(NtfsInfo left, NtfsInfo right)
        {
            return !left?.Equals(right) ?? true;
        }

        /// <inheritdoc cref="IEquatable{T}.Equals(object)"/>
        public bool Equals(NtfsInfo other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return StreamType == other.StreamType && StreamAttributes == other.StreamAttributes && Size == other.Size && Name == other.Name;
        }

        /// <inheritdoc cref="IEquatable{T}.Equals(object)"/>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((NtfsInfo)obj);
        }

        /// <inheritdoc cref="IEquatable{T}.GetHashCode"/>
        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (int)StreamType;
                hashCode = (hashCode * 397) ^ (int)StreamAttributes;
                hashCode = (hashCode * 397) ^ Size.GetHashCode();
                hashCode = (hashCode * 397) ^ (Name != null ? Name.GetHashCode() : 0);
                return hashCode;
            }
        }
    }

    /// <summary>
    /// Represents the attributes of an NTFS file stream.
    /// </summary>
    [Serializable]
    [Flags]
    public enum NtfsAttributes
    {
        /// <summary>
        /// No attributes.
        /// </summary>
        None = 0,

        /// <summary>
        /// The stream contains data that changes when read.
        /// </summary>
        ModifiedWhenRead = 1,

        /// <summary>
        /// The stream contains security data.
        /// </summary>
        ContainsSecurity = 2,

        /// <summary>
        /// The stream contains properties.
        /// </summary>
        ContainsProperties = 4,

        /// <summary>
        /// Set if the stream is sparse.
        /// </summary>
        Sparse = 8,
    }

    /// <summary>
    /// Represents the type of data in an NTFS stream.
    /// </summary>
    [Serializable]
    public enum NtfsType
    {
        /// <summary>
        /// Unknown stream type.
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// Standard data.
        /// </summary>
        Data = 1,

        /// <summary>
        /// Extended attribute data.
        /// </summary>
        ExtendedAttributes = 2,

        /// <summary>
        /// Security data.
        /// </summary>
        SecurityData = 3,

        /// <summary>
        /// Alternate data stream.
        /// </summary>
        AlternateDataStream = 4,

        /// <summary>
        /// Hard link information.
        /// </summary>
        Link = 5,

        /// <summary>
        /// File properties.
        /// </summary>
        PropertyData = 6,

        /// <summary>
        /// Object identifiers.
        /// </summary>
        ObjectId = 7,

        /// <summary>
        /// Restore point.
        /// </summary>
        ReparseData = 8,

        /// <summary>
        /// Sparse file.
        /// </summary>
        SparseBlock = 9,

        /// <summary>
        /// Transactional data. (Undocumented - BACKUP_TXFS_DATA)
        /// </summary>
        TransactionData = 10,
    }

    /// <summary>
    /// Transfer zone identifier.
    /// </summary>
    public enum ZoneId : int
    {
        /// <summary>
        /// Local computer.
        /// </summary>
        LocalMachine = 0,
        /// <summary>
        /// Local network.
        /// </summary>
        LocalIntranet = 1,
        /// <summary>
        /// Trusted sites.
        /// </summary>
        TrustedSites = 2,
        /// <summary>
        /// Internet. File opening is only available with user confirmation.
        /// </summary>
        Internet = 3,
        /// <summary>
        /// Dangerous sites. File opening is blocked.
        /// </summary>
        RestrictedSites = 4,
        /// <summary>
        /// Value is undefined.
        /// </summary>
        Undefined = -1
    }

    #endregion

    /// <summary>
    /// Provides a <see cref="Stream" /> in a file,
    /// supporting NTFS streams, as well as synchronous and asynchronous read and write operations.
    /// </summary>
    public class NtfsFileStream : FileStream
    {
        #region Private fields

        private const int MAX_PATH = 256;
        private const string LONG_PATH_PREFIX = @"\\?\";
        private const char STREAM_SEP = ':';
        private const int BUFF_SIZE = 4096;

        private string _fullPath;
        private string _filePath;
        private string _streamName;

        #endregion

        #region Win32 API imports

        [StructLayout(LayoutKind.Sequential)]
        private struct LargeInteger
        {
            public readonly int Low;
            public readonly int High;

            public LargeInteger(long value)
            {
                Low = (int)(value & 0x11111111);
                High = (int)((value / 0x100000000) & 0x11111111);
            }

            public long ToInt64()
            {
                return (High * 0x100000000) + Low;
            }

            public static LargeInteger FromInt64(long value)
            {
                return new LargeInteger(value);
            }

        }

        [StructLayout(LayoutKind.Sequential)]
        private struct Win32StreamId
        {
            public readonly int StreamId;
            public readonly int StreamAttributes;
            public LargeInteger Size;
            public readonly int StreamNameSize;
        }

        [Flags]
        private enum NativeFileFlags : uint
        {
            WriteThrough = 0x80000000,
            Overlapped = 0x40000000,
            NoBuffering = 0x20000000,
            RandomAccess = 0x10000000,
            SequentialScan = 0x8000000,
            DeleteOnClose = 0x4000000,
            BackupSemantics = 0x2000000,
            PosixSemantics = 0x1000000,
            OpenReparsePoint = 0x200000,
            OpenNoRecall = 0x100000
        }

        [Flags]
        private enum NativeFileAccess : uint
        {
            GenericRead = 0x80000000,
            GenericWrite = 0x40000000
        }

        [SuppressUnmanagedCodeSecurity]
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, BestFitMapping = false, ThrowOnUnmappableChar = true)]
        private static extern int FormatMessage(
            int dwFlags,
            IntPtr lpSource,
            int dwMessageId,
            int dwLanguageId,
            StringBuilder lpBuffer,
            int nSize,
            IntPtr vaListArguments);

        [SuppressUnmanagedCodeSecurity]
        [DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern int GetFileAttributes(string fileName);

        [SuppressUnmanagedCodeSecurity]
        [DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetFileSizeEx(SafeFileHandle handle, out LargeInteger size);

        [SuppressUnmanagedCodeSecurity]
        [DllImport("kernel32.dll")]
        private static extern int GetFileType(SafeFileHandle handle);

        [SuppressUnmanagedCodeSecurity]
        [DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern SafeFileHandle CreateFile(
            string name,
            NativeFileAccess access,
            FileShare share,
            IntPtr security,
            FileMode mode,
            NativeFileFlags flags,
            IntPtr template);

        [SuppressUnmanagedCodeSecurity]
        [DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern SafeFileHandle CreateFile(
            string name,
            NativeFileAccess access,
            FileShare share,
            IntPtr security,
            FileMode mode,
            int flags,
            IntPtr template);

        [DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DeleteFile(string name);

        [SuppressUnmanagedCodeSecurity]
        [DllImport("kernel32", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool BackupRead(
            SafeFileHandle hFile,
            ref Win32StreamId pBuffer,
            int numberOfBytesToRead,
            out int numberOfBytesRead,
            [MarshalAs(UnmanagedType.Bool)] bool abort,
            [MarshalAs(UnmanagedType.Bool)] bool processSecurity,
            ref IntPtr context);

        [SuppressUnmanagedCodeSecurity]
        [DllImport("kernel32", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool BackupRead(
            SafeFileHandle hFile,
            SafeHGlobalHandle pBuffer,
            int numberOfBytesToRead,
            out int numberOfBytesRead,
            [MarshalAs(UnmanagedType.Bool)] bool abort,
            [MarshalAs(UnmanagedType.Bool)] bool processSecurity,
            ref IntPtr context);

        [SuppressUnmanagedCodeSecurity]
        [DllImport("kernel32", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool BackupSeek(
            SafeFileHandle hFile,
            int bytesToSeekLow,
            int bytesToSeekHigh,
            out int bytesSeekedLow,
            out int bytesSeekedHigh,
            ref IntPtr context);

        #endregion

        #region Safe native methods

        [SecuritySafeCritical]
        private static SafeFileHandle SafeCreateFile(string path, NativeFileAccess access, FileShare share, IntPtr security, FileMode mode, NativeFileFlags flags, IntPtr template)
        {
            SafeFileHandle result = CreateFile(path, access, share, security, mode, flags, template);
            if (!result.IsInvalid && 1 != GetFileType(result))
            {
                result.Dispose();
                throw new NotSupportedException();
            }

            return result;
        }

        [SecuritySafeCritical]
        private static SafeFileHandle SafeCreateFile(string path, NativeFileAccess access, FileShare share, IntPtr security, FileMode mode, int flags, IntPtr template)
        {
            SafeFileHandle result = CreateFile(path, access, share, security, mode, flags, template);
            if (!result.IsInvalid && 1 != GetFileType(result))
            {
                result.Dispose();
                throw new NotSupportedException();
            }

            return result;
        }

        [SecuritySafeCritical]
        private static SafeFileHandle SafeCreateFile(string filePath, string streamName, FileMode mode, FileAccess access, FileShare share, int bufferSize = BUFF_SIZE, bool isAsync = false)
        {
            if (Environment.OSVersion.Platform == PlatformID.Win32Windows)
                throw new PlatformNotSupportedException();
            FileShare fileShare = share & ~FileShare.Inheritable;
            if (mode < FileMode.CreateNew || mode > FileMode.Append)
                throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
            if (fileShare < FileShare.None || fileShare > (FileShare.ReadWrite | FileShare.Delete))
                throw new ArgumentOutOfRangeException(nameof(fileShare), fileShare, null);
            if (filePath == null)
                throw new ArgumentNullException(nameof(filePath));
            if (filePath == string.Empty)
                throw new ArgumentException(null, nameof(filePath));
            if (bufferSize < 0)
                throw new ArgumentOutOfRangeException(nameof(bufferSize), bufferSize, null);

            if (mode == FileMode.Truncate)
                access = FileAccess.Write;

            string fullPath = string.IsNullOrEmpty(streamName) ? filePath : BuildStreamPath(filePath, streamName);
            FileIOPermissionAccess permAccess = CalculateAccess(mode, access);
            new FileIOPermission(permAccess, filePath).Demand();

            NativeFileFlags flags = isAsync ? NativeFileFlags.Overlapped : 0;
            NativeFileAccess nativeFileAccess = FileAccessToNative(access);
            SafeFileHandle handle = SafeCreateFile(fullPath, nativeFileAccess, share, IntPtr.Zero, mode, flags, IntPtr.Zero);
            if (handle.IsInvalid)
            {
                int errorCode = Marshal.GetLastWin32Error();
                if (errorCode != 0)
                {
                    string message = GetErrorMessage(errorCode);
                    throw new Win32Exception(errorCode, $"{filePath} - {message}");
                }
            }

            return handle;
        }

        [SecuritySafeCritical]
        private static SafeFileHandle SafeCreateFile(string filePath, string streamName, FileMode mode, FileAccess access, FileShare share, FileOptions options, int bufferSize = BUFF_SIZE, bool isAsync = false)
        {
            if (Environment.OSVersion.Platform == PlatformID.Win32Windows)
                throw new PlatformNotSupportedException();
            FileShare fileShare = share & ~FileShare.Inheritable;
            if (mode < FileMode.CreateNew || mode > FileMode.Append)
                throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
            if (fileShare < FileShare.None || fileShare > (FileShare.ReadWrite | FileShare.Delete))
                throw new ArgumentOutOfRangeException(nameof(fileShare), fileShare, null);
            if (filePath == null)
                throw new ArgumentNullException(nameof(filePath));
            if (filePath == string.Empty)
                throw new ArgumentException(null, nameof(filePath));
            if (options != FileOptions.None && (options & (FileOptions)67092479) != FileOptions.None)
                throw new ArgumentOutOfRangeException(nameof(options), options, null);
            if (bufferSize < 0)
                throw new ArgumentOutOfRangeException(nameof(bufferSize), bufferSize, null);

            if (mode == FileMode.Truncate)
                access = FileAccess.Write;

            string fullPath = string.IsNullOrEmpty(streamName) ? filePath : BuildStreamPath(filePath, streamName);
            int dwFlagsAndAttributes = (int)(options | (FileOptions)1048576);
            FileIOPermissionAccess permAccess = CalculateAccess(mode, access);
            new FileIOPermission(permAccess, filePath).Demand();

            NativeFileFlags flags = (NativeFileFlags)(options | (FileOptions)1048576);
            NativeFileAccess nativeFileAccess = FileAccessToNative(access);
            SafeFileHandle handle = SafeCreateFile(fullPath, nativeFileAccess, share, IntPtr.Zero, mode, dwFlagsAndAttributes, IntPtr.Zero);
            if (handle.IsInvalid)
            {
                int errorCode = Marshal.GetLastWin32Error();
                if (errorCode != 0)
                {
                    string message = GetErrorMessage(errorCode);
                    throw new Win32Exception(errorCode, $"{fullPath} - {message}");
                }
            }

            return handle;
        }

        [SecuritySafeCritical]
        private static SafeFileHandle SafeCreateFile(string filePath, string streamName, FileMode mode, FileAccess access, FileShare share, FileSystemRights fileSystemRights, FileOptions options, int bufferSize = BUFF_SIZE, bool isAsync = false)
        {
            bool useRights = true;
            if (Environment.OSVersion.Platform == PlatformID.Win32Windows)
                throw new PlatformNotSupportedException();
            FileShare fileShare = share & ~FileShare.Inheritable;
            if (mode < FileMode.CreateNew || mode > FileMode.Append)
                throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
            if (fileShare < FileShare.None || fileShare > (FileShare.ReadWrite | FileShare.Delete))
                throw new ArgumentOutOfRangeException(nameof(fileShare), fileShare, null);
            if (filePath == null)
                throw new ArgumentNullException(nameof(filePath));
            if (filePath == string.Empty)
                throw new ArgumentException(null, nameof(filePath));
            if (options != FileOptions.None && (options & (FileOptions)67092479) != FileOptions.None)
                throw new ArgumentOutOfRangeException(nameof(options), options, null);
            if (fileSystemRights < FileSystemRights.ReadData || fileSystemRights > FileSystemRights.FullControl)
                throw new ArgumentOutOfRangeException(nameof(fileSystemRights), fileSystemRights, null);
            if (bufferSize < 0)
                throw new ArgumentOutOfRangeException(nameof(bufferSize), bufferSize, null);

            if (mode == FileMode.Truncate)
            {
                useRights = false;
                access = FileAccess.Write;
            }

            string fullPath = string.IsNullOrEmpty(streamName) ? filePath : BuildStreamPath(filePath, streamName);
            FileIOPermissionAccess permAccess = CalculateAccess(mode, access);
            new FileIOPermission(permAccess, filePath).Demand();

            NativeFileFlags nativeFileflags = (NativeFileFlags)(options | (FileOptions)1048576);
            NativeFileAccess nativeFileAccess = useRights ? FileAccessToNative(access) : (NativeFileAccess)fileSystemRights;
            SafeFileHandle handle = SafeCreateFile(fullPath, nativeFileAccess, share, IntPtr.Zero, mode, nativeFileflags, IntPtr.Zero);
            if (handle.IsInvalid)
            {
                int errorCode = Marshal.GetLastWin32Error();
                if (errorCode != 0)
                {
                    string message = GetErrorMessage(errorCode);
                    throw new Win32Exception(errorCode, $"{filePath} - {message}");
                }
            }

            return handle;
        }

        [SecuritySafeCritical]
        private static int SafeGetFileAttributes(string filePath)
        {
            if (string.IsNullOrEmpty(filePath)) throw new ArgumentNullException(nameof(filePath));

            int result = GetFileAttributes(filePath);
            if (-1 == result)
            {
                int errorCode = Marshal.GetLastWin32Error();
                string message = GetErrorMessage(errorCode);
                throw new Win32Exception(errorCode, $"{filePath} - {message}");
            }

            return result;
        }

        [SecuritySafeCritical]
        private static bool SafeDeleteFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath)) throw new ArgumentNullException(nameof(filePath));

            bool result = DeleteFile(filePath);
            if (!result)
            {
                int errorCode = Marshal.GetLastWin32Error();
                string message = GetErrorMessage(errorCode);
                throw new Win32Exception(errorCode, $"{filePath} - {message}");
            }

            return result;
        }

        #endregion

        #region Tools

        private static FileIOPermissionAccess CalculateAccess(FileMode mode, FileAccess access)
        {
            var permAccess = FileIOPermissionAccess.NoAccess;
            switch (mode)
            {
                case FileMode.Append:
                    permAccess = FileIOPermissionAccess.Append;
                    break;

                case FileMode.Create:
                case FileMode.CreateNew:
                case FileMode.OpenOrCreate:
                case FileMode.Truncate:
                    permAccess = FileIOPermissionAccess.Write;
                    break;

                case FileMode.Open:
                    permAccess = FileIOPermissionAccess.Read;
                    break;
            }
            switch (access)
            {
                case FileAccess.ReadWrite:
                    permAccess |= FileIOPermissionAccess.Write;
                    permAccess |= FileIOPermissionAccess.Read;
                    break;

                case FileAccess.Write:
                    permAccess |= FileIOPermissionAccess.Write;
                    break;

                case FileAccess.Read:
                    permAccess |= FileIOPermissionAccess.Read;
                    break;
            }

            return permAccess;
        }

        private static NativeFileAccess FileAccessToNative(FileAccess access)
        {
            NativeFileAccess result = 0;
            if (FileAccess.Read == (FileAccess.Read & access)) result |= NativeFileAccess.GenericRead;
            if (FileAccess.Write == (FileAccess.Write & access)) result |= NativeFileAccess.GenericWrite;
            return result;
        }

        public static string GetErrorMessage(int errorCode)
        {
            var lpBuffer = new StringBuilder(0x200);
            if (0 != FormatMessage(0x3200, IntPtr.Zero, errorCode, 0, lpBuffer, lpBuffer.Capacity, IntPtr.Zero))
            {
                return lpBuffer.ToString();
            }

            return string.Format($"Unknown error {errorCode}.");
        }

        private static string BuildStreamPath(string filePath, string streamName)
        {
            string result = filePath;
            if (!string.IsNullOrEmpty(filePath))
            {
                if (result.Length == 1) result = ".\\" + result;
                result += STREAM_SEP + streamName + STREAM_SEP + "$DATA";
                if (result.Length >= MAX_PATH) result = LONG_PATH_PREFIX + result;
            }
            return result;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Absolute or relative path to the NTFS file stream.
        /// </summary>
        public string FullPath
        {
            get { return _fullPath; }
        }

        public string FilePath
        {
            get { return _filePath; }
        }

        public string Name
        {
            get { return _streamName; }
        }

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="System.IO.NtfsFileStream" />
        /// class with the specified path and creation mode.
        /// </summary>
        /// <param name="filePath">
        /// Absolute or relative path to the NTFS file stream
        /// that will be encapsulated by the <see cref="NtfsFileStream" /> object.
        /// </param>
        /// <param name="streamName">Name of the NTFS stream.</param>
        /// <param name="mode">
        /// A constant that determines how to open or create the file.
        /// </param>
        public NtfsFileStream(string filePath, string streamName, FileMode mode)
            : this(filePath, streamName, mode, mode == FileMode.Append ? FileAccess.Write : FileAccess.ReadWrite, FileShare.Read, BUFF_SIZE, false)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NtfsFileStream" /> class with the specified path,
        /// creation mode, and read/write permissions.
        /// </summary>
        /// <param name="filePath">
        /// Absolute or relative path to the file that will be
        /// encapsulated by the <see cref="NtfsFileStream" /> object.
        /// </param>
        /// <param name="streamName">Name of the NTFS stream.</param>
        /// <param name="mode">
        /// A constant that determines how to open or create the file.
        /// </param>
        /// <param name="access">
        /// A constant that determines how the <see cref="NtfsFileStream" /> object can access the file.
        /// Also determines the values returned by the <see cref="FileStream.CanRead" />
        /// and <see cref="FileStream.CanWrite" /> properties of the <see cref="NtfsFileStream" /> object.
        /// The <see cref="FileStream.CanSeek" /> property is <see langword="true" />,
        /// if the <paramref name="filePath" /> parameter specifies a file on disk.
        /// </param>
        public NtfsFileStream(string filePath, string streamName, FileMode mode, FileAccess access)
            : base(SafeCreateFile(filePath, streamName, mode, access, FileShare.Read), access, BUFF_SIZE, false)
        {

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NtfsFileStream" /> class with the specified path, creation mode,
        /// read/write permissions, and sharing permissions.
        /// </summary>
        /// <param name="filePath">
        /// Absolute or relative path to the file that will be
        /// encapsulated by the <see cref="NtfsFileStream" /> object.
        /// </param>
        /// <param name="streamName">Name of the NTFS stream.</param>
        /// <param name="mode">
        /// A constant that determines how to open or create the file.
        /// </param>
        /// <param name="access">
        /// A constant that determines how the <see cref="NtfsFileStream" /> object can access the file.
        /// Also determines the values returned by the <see cref="FileStream.CanRead" />
        /// and <see cref="FileStream.CanWrite" /> properties of the <see cref="NtfsFileStream" /> object.
        /// The <see cref="FileStream.CanSeek" /> property is <see langword="true" />,
        /// if the <paramref name="filePath" /> parameter specifies a file on disk.
        /// </param>
        /// <param name="share">
        /// A constant that determines how the file will be shared by processes.
        /// </param>
        public NtfsFileStream(string filePath, string streamName, FileMode mode, FileAccess access, FileShare share)
            : base(SafeCreateFile(filePath, streamName, mode, access, share), access, BUFF_SIZE, false)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NtfsFileStream" /> class with the specified path, creation mode,
        /// read/write permissions, sharing permissions, and buffer size.
        /// </summary>
        /// <param name="filePath">
        /// Absolute or relative path to the file that will be
        /// encapsulated by the <see cref="NtfsFileStream" /> object.
        /// </param>
        /// <param name="streamName">Name of the NTFS stream.</param>
        /// <param name="mode">
        /// A constant that determines how to open or create the file.
        /// </param>
        /// <param name="access">
        /// A constant that determines how the <see cref="NtfsFileStream" /> object can access the file.
        /// Also determines the values returned by the <see cref="NtfsFileStream.CanRead" />
        /// and <see cref="FileStream.CanWrite" /> properties of the <see cref="NtfsFileStream" /> object.
        /// The <see cref="FileStream.CanSeek" /> property is <see langword="true" />,
        /// if the <paramref name="filePath" /> parameter specifies a file on disk.
        /// </param>
        /// <param name="share">
        /// A constant that determines how the file will be shared by processes.
        /// </param>
        /// <param name="bufferSize">
        /// A positive <see cref="Int32" /> value greater than 0 that specifies the buffer size.
        /// The default buffer size is 4096.
        /// </param>
        public NtfsFileStream(string filePath, string streamName, FileMode mode, FileAccess access, FileShare share, int bufferSize)
            : base(SafeCreateFile(filePath, streamName, mode, access, share), access, bufferSize, false)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NtfsFileStream" /> class with the specified path,
        /// creation mode, read/write permissions, sharing permissions, access for other FileStreams to the same file,
        /// buffer size, and additional file options.
        /// </summary>
        /// <param name="filePath">
        /// Absolute or relative path to the file that will be
        /// encapsulated by the <see cref="NtfsFileStream" /> object.
        /// </param>
        /// <param name="streamName">Name of the NTFS stream.</param>
        /// <param name="mode">
        /// A constant that determines how to open or create the file.
        /// </param>
        /// <param name="access">
        /// A constant that determines how the <see cref="NtfsFileStream" /> object can access the file.
        /// Also determines the values returned by the <see cref="FileStream.CanRead" />
        /// and <see cref="FileStream.CanWrite" /> properties of the <see cref="NtfsFileStream" /> object.
        /// The <see cref="FileStream.CanSeek" /> property is <see langword="true" />,
        /// if the <paramref name="filePath" /> parameter specifies a file on disk.
        /// </param>
        /// <param name="share">
        /// A constant that determines how the file will be shared by processes.
        /// </param>
        /// <param name="bufferSize">
        /// A positive <see cref="Int32" /> value greater than 0 that specifies the buffer size.
        /// The default buffer size is 4096.
        /// </param>
        /// <param name="options">
        /// A value that specifies additional file options.
        /// </param>
        public NtfsFileStream(string filePath, string streamName, FileMode mode, FileAccess access, FileShare share, int bufferSize, FileOptions options)
            : base(SafeCreateFile(filePath, streamName, mode, access, share, options), access, bufferSize, false)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NtfsFileStream" /> class with the specified path, creation mode,
        /// read/write permissions, sharing permissions, buffer size, and synchronous
        /// or asynchronous state.
        /// </summary>
        /// <param name="filePath">
        /// Absolute or relative path to the file that will be
        /// encapsulated by the <see cref="NtfsFileStream" /> object.
        /// </param>
        /// <param name="streamName">Name of the NTFS stream.</param>
        /// <param name="mode">
        /// A constant that determines how to open or create the file.
        /// </param>
        /// <param name="access">
        /// A constant that determines how the <see cref="NtfsFileStream" /> object can access the file.
        /// Also determines the values returned by the <see cref="FileStream.CanRead" />
        /// and <see cref="FileStream.CanWrite" /> properties of the <see cref="NtfsFileStream" /> object.
        /// The <see cref="FileStream.CanSeek" /> property is <see langword="true" />,
        /// if the <paramref name="filePath" /> parameter specifies a file on disk.
        /// </param>
        /// <param name="share">
        /// A constant that determines how the file will be shared by processes.
        /// </param>
        /// <param name="bufferSize">
        /// A positive <see cref="Int32" /> value greater than 0 that specifies the buffer size.
        /// The default buffer size: 4096.
        /// </param>
        /// <param name="useAsync">
        /// Specifies whether to use asynchronous I/O or synchronous I/O.
        /// However, note that the underlying operating system may not support asynchronous I/O,
        /// so when <see langword="true" /> is specified, the handle may be opened synchronously
        /// depending on the platform.
        /// When opened asynchronously, the methods <see cref="FileStream.BeginRead(System.Byte[],System.Int32,System.Int32,System.AsyncCallback,System.Object)" />
        /// and <see cref="FileStream.BeginWrite(System.Byte[],System.Int32,System.Int32,System.AsyncCallback,System.Object)" />
        /// perform better when reading or writing large amounts, but they may work much slower
        /// when reading or writing small amounts of data.
        /// If the application is designed to take advantage of asynchronous I/O,
        /// set the <paramref name="useAsync" /> parameter to <see langword="true" />.
        /// When asynchronous I/O is used correctly, application performance
        /// can increase up to 10 times, but using such I/O mode without reworking
        /// the application can degrade performance by the same factor.
        /// </param>
        public NtfsFileStream(string filePath, string streamName, FileMode mode, FileAccess access, FileShare share, int bufferSize, bool useAsync)
            : base(SafeCreateFile(filePath, streamName, mode, access, share), access, bufferSize, useAsync)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NtfsFileStream" /> class with the specified path, creation mode,
        /// read/write permissions, sharing permissions, buffer size, and additional file options.
        /// </summary>
        /// <param name="filePath">
        /// Absolute or relative path to the file that will be
        /// encapsulated by the <see cref="NtfsFileStream" /> object.
        /// </param>
        /// <param name="streamName">Name of the NTFS stream.</param>
        /// <param name="mode">
        /// A constant that determines how to open or create the file.
        /// </param>
        /// <param name="rights">
        /// A constant that determines the access rights used when creating access and audit rules for the file.
        /// </param>
        /// <param name="share">
        /// A constant that determines how the file will be shared by processes.
        /// </param>
        /// <param name="bufferSize">
        /// A positive <see cref="Int32" /> value greater than 0 that specifies the buffer size.
        /// The default buffer size is 4096.
        /// </param>
        /// <param name="options">
        /// A constant that specifies additional file options.
        /// </param>
        public NtfsFileStream(string filePath, string streamName, FileMode mode, FileSystemRights rights, FileShare share, int bufferSize, FileOptions options)
            : base(SafeCreateFile(filePath, streamName, mode, (FileAccess)0, share, rights, options), (FileAccess)0, bufferSize, false)
        {
        }

        #endregion

        #region Public static methods

        /// <summary>
        /// Opens a <see cref="NtfsFileStream" /> at the specified path,
        /// using the specified parameters.
        /// </summary>
        /// <param name="filePath">The file to open.</param>
        /// <param name="mode">
        /// A <see cref="T:System.IO.FileMode" /> value that specifies
        /// whether a file should be created if it does not exist, and determines whether the contents of existing
        /// files are preserved or overwritten.
        /// </param>
        /// <param name="access">
        /// A <see cref="T:System.IO.FileAccess" /> value that describes the operations that can be performed on the file.
        /// </param>
        /// <param name="share">
        /// A <see cref="T:System.IO.FileShare" /> value that specifies the type of access other threads have to the file.
        /// </param>
        /// <param name="bufferSize">The buffer size in bytes.</param>
        /// <param name="isAsync">Specifies whether to use asynchronous I/O or synchronous I/O.</param>
        /// <returns>
        /// A <see cref="NtfsFileStream" /> stream at the specified path with the specified access mode for reading,
        /// writing, or reading and writing, and with the specified sharing option.
        /// </returns>
        public static NtfsFileStream Open(string filePath, FileMode mode, FileAccess access,
            FileShare share, int bufferSize = BUFF_SIZE, bool isAsync = false)
        {
            return new NtfsFileStream(filePath, null, mode, access, share, bufferSize, isAsync);
        }

        /// <summary>
        /// Opens a <see cref="NtfsFileStream" /> at the specified path,
        /// using the specified parameters.
        /// </summary>
        /// <param name="filePath">The file to open.</param>
        /// <param name="streamName">Name of the NTFS stream associated with the file to open.</param>
        /// <param name="mode">
        /// A <see cref="T:System.IO.FileMode" /> value that specifies
        /// whether a file should be created if it does not exist, and determines whether the contents of existing files are preserved
        /// or overwritten.
        /// </param>
        /// <param name="access">
        /// A <see cref="T:System.IO.FileAccess" /> value that describes the operations that can be performed on the file.
        /// </param>
        /// <param name="share">
        /// A <see cref="T:System.IO.FileShare" /> value that specifies the type of access other threads have to the file.
        /// </param>
        /// <param name="bufferSize">The buffer size in bytes.</param>
        /// <param name="isAsync">Specifies whether to use asynchronous I/O or synchronous I/O.</param>
        /// <returns>
        /// A <see cref="NtfsFileStream" /> stream at the specified path with the specified access mode for reading,
        /// writing, or reading and writing, and with the specified sharing option.
        /// </returns>
        public static NtfsFileStream Open(string filePath, string streamName, FileMode mode,
            FileAccess access, FileShare share, int bufferSize = BUFF_SIZE, bool isAsync = false)
        {
            return new NtfsFileStream(filePath, streamName, mode, access, share, bufferSize, isAsync);
        }

        /// <summary>
        /// Opens a <see cref="NtfsFileStream" /> at the specified path,
        /// using the specified parameters.
        /// </summary>
        /// <param name="filePath">The file to open.</param>
        /// <param name="streamInfo">Properties of the NTFS stream associated with the file to open.</param>
        /// <param name="mode">
        /// A <see cref="T:System.IO.FileMode" /> value that specifies
        /// whether a file should be created if it does not exist, and determines whether the contents
        /// of existing files are preserved or overwritten.
        /// </param>
        /// <param name="access">
        /// A <see cref="T:System.IO.FileAccess" /> value that describes the operations that can be performed on the file.
        /// </param>
        /// <param name="share">
        /// A <see cref="T:System.IO.FileShare" /> value that specifies the type of access other threads have to the file.
        /// </param>
        /// <param name="bufferSize">The buffer size in bytes.</param>
        /// <param name="isAsync">Specifies whether to use asynchronous I/O or synchronous I/O.</param>
        /// <returns>
        /// A <see cref="NtfsFileStream" /> stream at the specified path with the specified access mode for reading,
        /// writing, or reading and writing, and with the specified sharing option.
        /// </returns>
        public static NtfsFileStream Open(string filePath, NtfsInfo streamInfo, FileMode mode, FileAccess access, FileShare share, int bufferSize = BUFF_SIZE, bool isAsync = false)
        {
            return new NtfsFileStream(filePath, streamInfo.Name, mode, access, share, bufferSize, isAsync);
        }

        /// <summary>Opens an existing file for reading.</summary>
        /// <param name="filePath">The file to open for reading.</param>
        /// <param name="bufferSize">The buffer size in bytes.</param>
        /// <param name="isAsync">Specifies whether to use asynchronous I/O or synchronous I/O.</param>
        /// <returns>
        /// A read-only <see cref="NtfsFileStream" /> at the specified path.
        /// </returns>
        public static NtfsFileStream OpenRead(string filePath, int bufferSize = BUFF_SIZE, bool isAsync = false)
        {
            return new NtfsFileStream(filePath, null, FileMode.Open, FileAccess.Read, FileShare.Read);
        }

        /// <summary>Opens an existing NTFS stream in the specified file for reading.</summary>
        /// <param name="filePath">The file to open for reading.</param>
        /// <param name="streamName">Name of the NTFS stream associated with the file to open.</param>
        /// <param name="bufferSize">The buffer size in bytes.</param>
        /// <param name="isAsync">Specifies whether to use asynchronous I/O or synchronous I/O.</param>
        /// <returns>
        /// A read-only <see cref="NtfsFileStream" /> at the specified path.
        /// </returns>
        public static NtfsFileStream OpenRead(string filePath, string streamName,
            int bufferSize = BUFF_SIZE, bool isAsync = false)
        {
            return new NtfsFileStream(filePath, streamName, FileMode.Open, FileAccess.Read, FileShare.Read);
        }

        /// <summary>Opens an existing NTFS stream in the specified file for reading.</summary>
        /// <param name="filePath">The file to open for reading.</param>
        /// <param name="streamInfo">Properties of the NTFS stream associated with the file to open.</param>
        /// <param name="bufferSize">The buffer size in bytes.</param>
        /// <param name="isAsync">Specifies whether to use asynchronous I/O or synchronous I/O.</param>
        /// <returns>
        /// A read-only <see cref="NtfsFileStream" /> at the specified path.
        /// </returns>
        public static NtfsFileStream OpenRead(string filePath, NtfsInfo streamInfo,
            int bufferSize = BUFF_SIZE, bool isAsync = false)
        {
            return new NtfsFileStream(filePath, streamInfo.Name, FileMode.Open, FileAccess.Read, FileShare.Read);
        }

        /// <summary>
        /// Opens an existing file or creates a new file for writing, using the specified parameters.
        /// </summary>
        /// <param name="filePath">The file to open for writing.</param>
        /// <param name="bufferSize">The buffer size in bytes.</param>
        /// <param name="isAsync">Specifies whether to use asynchronous I/O or synchronous I/O.</param>
        /// <returns>
        /// An object with exclusive access <see cref="NtfsFileStream" /> at the specified path with <see cref="FileAccess.Write" /> access.
        /// </returns>
        public static NtfsFileStream OpenWrite(string filePath, int bufferSize = BUFF_SIZE, bool isAsync = false)
        {
            return new NtfsFileStream(filePath, null, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None);
        }

        /// <summary>
        /// Opens an existing NTFS stream or creates a new one for writing, using the specified parameters.
        /// </summary>
        /// <param name="filePath">The file to open for writing.</param>
        /// <param name="streamName">Name of the NTFS stream associated with the file to open.</param>
        /// <param name="bufferSize">The buffer size in bytes.</param>
        /// <param name="isAsync">Specifies whether to use asynchronous I/O or synchronous I/O.</param>
        /// <returns>
        /// An object with exclusive access <see cref="NtfsFileStream" /> at the specified path with <see cref="FileAccess.Write" /> access.
        /// </returns>
        public static NtfsFileStream OpenWrite(string filePath, string streamName, int bufferSize = BUFF_SIZE, bool isAsync = false)
        {
            return new NtfsFileStream(filePath, streamName, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None);
        }

        /// <summary>
        /// Opens an existing NTFS stream or creates a new one for writing, using the specified parameters.
        /// </summary>
        /// <param name="filePath">The file to open for writing.</param>
        /// <param name="streamInfo">Properties of the NTFS stream associated with the file to open.</param>
        /// <param name="bufferSize">The buffer size in bytes.</param>
        /// <param name="isAsync">Specifies whether to use asynchronous I/O or synchronous I/O.</param>
        /// <returns>
        /// An object with exclusive access <see cref="NtfsFileStream" /> at the specified path with <see cref="FileAccess.Write" /> access.
        /// </returns>
        public static NtfsFileStream OpenWrite(string filePath, NtfsInfo streamInfo, int bufferSize = BUFF_SIZE, bool isAsync = false)
        {
            return new NtfsFileStream(filePath, streamInfo.Name, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None);
        }

        /// <summary>
        /// Creates or overwrites the specified file, using the specified parameters.
        /// </summary>
        /// <param name="filePath">The file to create.</param>
        /// <param name="options">One of the <see cref="FileOptions" /> values that describes
        /// how to create or overwrite the file.</param>
        /// <param name="bufferSize">The buffer size in bytes.</param>
        /// <returns>
        /// An object with exclusive access <see cref="NtfsFileStream" /> at the specified path with <see cref="FileAccess.Write" /> access.
        /// </returns>
        public static NtfsFileStream Create(string filePath, FileOptions options, int bufferSize = BUFF_SIZE)
        {
            return new NtfsFileStream(filePath, null, FileMode.Create, FileAccess.ReadWrite, FileShare.None, bufferSize, options);
        }

        /// <summary>
        /// Creates or overwrites the specified NTFS stream in a file, using the specified parameters.
        /// </summary>
        /// <param name="filePath">The file to open for writing.</param>
        /// <param name="streamName">Name of the NTFS stream associated with the file to open.</param>
        /// <param name="options">One of the <see cref="FileOptions" /> values that describes
        /// how to create or overwrite the file.</param>
        /// <param name="bufferSize">The buffer size in bytes.</param>
        /// <returns>
        /// An object with exclusive access <see cref="NtfsFileStream" /> at the specified path with <see cref="FileAccess.Write" /> access.
        /// </returns>
        public static NtfsFileStream Create(string filePath, string streamName, FileOptions options, int bufferSize = BUFF_SIZE)
        {
            return new NtfsFileStream(filePath, streamName, FileMode.Create, FileAccess.ReadWrite, FileShare.None, bufferSize, options);
        }

        /// <summary>
        /// Creates or overwrites the specified NTFS stream in a file, using the specified parameters.
        /// </summary>
        /// <param name="filePath">The file to open for writing.</param>
        /// <param name="streamInfo">Properties of the NTFS stream associated with the file to open.</param>
        /// <param name="options">One of the <see cref="FileOptions" /> values that describes
        /// how to create or overwrite the file.</param>
        /// <param name="bufferSize">The buffer size in bytes.</param>
        /// <returns>
        /// An object with exclusive access <see cref="NtfsFileStream" /> at the specified path with <see cref="FileAccess.Write" /> access.
        /// </returns>
        public static NtfsFileStream Create(string filePath, NtfsInfo streamInfo, FileOptions options, int bufferSize = BUFF_SIZE)
        {
            return new NtfsFileStream(filePath, streamInfo.Name, FileMode.Create, FileAccess.ReadWrite, FileShare.None, bufferSize, options);
        }

        /// <summary>
        /// Creates or overwrites the specified file, using the specified parameters.
        /// </summary>
        /// <param name="filePath">The file to create.</param>
        /// <param name="bufferSize">The buffer size in bytes.</param>
        /// <returns>
        /// An object with exclusive access <see cref="NtfsFileStream" /> at the specified path with <see cref="FileAccess.Write" /> access.
        /// </returns>
        public static NtfsFileStream Create(string filePath, int bufferSize = BUFF_SIZE)
        {
            return new NtfsFileStream(filePath, null, FileMode.Create, FileAccess.ReadWrite, FileShare.None, bufferSize);
        }

        /// <summary>
        /// Creates or overwrites the specified NTFS stream in a file, using the specified parameters.
        /// </summary>
        /// <param name="filePath">The file to open for writing.</param>
        /// <param name="streamName">Name of the NTFS stream associated with the file to open.</param>
        /// <param name="bufferSize">The buffer size in bytes.</param>
        /// <returns>
        /// An object with exclusive access <see cref="NtfsFileStream" /> at the specified path with <see cref="FileAccess.Write" /> access.
        /// </returns>
        public static NtfsFileStream Create(string filePath, string streamName, int bufferSize = BUFF_SIZE)
        {
            return new NtfsFileStream(filePath, streamName, FileMode.Create, FileAccess.ReadWrite, FileShare.None, bufferSize);
        }

        /// <summary>
        /// Creates or overwrites the specified NTFS stream in a file, using the specified parameters.
        /// </summary>
        /// <param name="filePath">The file to open for writing.</param>
        /// <param name="streamInfo">Properties of the NTFS stream associated with the file to open.</param>
        /// <param name="bufferSize">The buffer size in bytes.</param>
        /// <returns>
        /// An object with exclusive access <see cref="NtfsFileStream" /> at the specified path with <see cref="FileAccess.Write" /> access.
        /// </returns>
        public static NtfsFileStream Create(string filePath, NtfsInfo streamInfo, int bufferSize = BUFF_SIZE)
        {
            return new NtfsFileStream(filePath, streamInfo.Name, FileMode.Create, FileAccess.ReadWrite, FileShare.None, bufferSize);
        }

        /// <summary>
        /// Opens an existing NTFS stream containing UTF-8 encoded text for reading.
        /// </summary>
        /// <param name="filePath">The file to open for reading.</param>
        /// <param name="streamName">Name of the NTFS stream associated with the file to open.</param>
        /// <returns>A <see cref="StreamReader" /> at the specified path.</returns>
        public static StreamReader OpenText(string filePath, string streamName)
        {
            NtfsFileStream stream = new NtfsFileStream(filePath, streamName, FileMode.Open, FileAccess.Read,
                FileShare.Read, BUFF_SIZE, FileOptions.SequentialScan);

            return new StreamReader(stream);
        }

        /// <summary>
        /// Opens an existing NTFS stream containing text in the specified encoding for reading.
        /// </summary>
        /// <param name="filePath">The file to open for reading.</param>
        /// <param name="streamName">Name of the NTFS stream associated with the file to open.</param>
        /// <param name="encoding">The character encoding in the stream to open.</param>
        /// <returns>A <see cref="StreamReader" /> at the specified path.</returns>
        public static StreamReader OpenText(string filePath, string streamName, Encoding encoding)
        {
            using (NtfsFileStream stream = new NtfsFileStream(filePath, streamName, FileMode.Open, FileAccess.Read,
                FileShare.Read, BUFF_SIZE, FileOptions.SequentialScan))
            {
                return new StreamReader(stream, encoding);
            }
        }

        /// <summary>
        /// Deletes the specified NTFS stream in the specified file.
        /// </summary>
        /// <param name="filePath">The path to the file in which to delete the stream.</param>
        /// <param name="streamName">Name of the NTFS stream associated with the specified file to delete.</param>
        /// <returns><see langword="true"/> if the operation was successful.</returns>
        public static bool Delete(string filePath, string streamName)
        {
            if (null == filePath) throw new ArgumentNullException(nameof(filePath));
            if (null == streamName) throw new ArgumentNullException(nameof(streamName));
            FileAttributes attributes = File.GetAttributes(filePath);
            if (attributes.HasFlag(FileAttributes.ReadOnly))
            {
                attributes &= ~FileAttributes.ReadOnly;
                File.SetAttributes(filePath, attributes);
            }
            const FileIOPermissionAccess permAccess = FileIOPermissionAccess.Write;
            new FileIOPermission(permAccess, filePath).Demand();
            string path = BuildStreamPath(filePath, streamName);

            if (!Exists(filePath, streamName))
                throw new FileNotFoundException(null, path);

            var result = false;
            if (SafeGetFileAttributes(path) != -1)
            {
                result = SafeDeleteFile(path);
            }

            return result;
        }

        /// <summary>
        /// Deletes the specified NTFS stream in the specified file.
        /// </summary>
        /// <param name="filePath">The path to the file in which to delete the stream.</param>
        /// <param name="streamInfo">Properties of the NTFS stream associated with the specified file to delete.</param>
        /// <returns><see langword="true"/> if the operation was successful.</returns>
        public static bool  Delete(string filePath, NtfsInfo streamInfo)
        {
            return Delete(filePath, streamInfo.Name);
        }

        /// <summary>
        /// Determines whether the specified file and its associated NTFS stream exist.
        /// </summary>
        /// <param name="filePath">The file to check.</param>
        /// <param name="streamName">The NTFS stream to check.</param>
        /// <returns>
        /// <see langword="true" /> if the specified file and its associated NTFS stream exist.</returns>
        public static bool Exists(string filePath, string streamName)
        {
            foreach (var info in NtfsFileStream.GetStreamsInfo(filePath))
            {
                if (info.Name == streamName) return true;
            }

            return false;
        }

        /// <summary>
        /// Determines whether the specified file and its associated NTFS stream exist.
        /// </summary>
        /// <param name="filePath">The file to check.</param>
        /// <param name="streamInfo">The NTFS stream to check.</param>
        /// <returns>
        /// <see langword="true" /> if the specified file and its associated NTFS stream exist.</returns>
        public static bool Exists(string filePath, NtfsInfo streamInfo)
        {
            foreach (var info in NtfsFileStream.GetStreamsInfo(filePath))
            {
                if (info == streamInfo) return true;
            }

            return false;
        }

        /// <summary>
        /// Returns information about all NTFS streams associated with the given file or directory.
        /// </summary>
        /// <param name="path">The path to the file or directory.</param>
        /// <returns>
        /// An array of <see cref="NtfsInfo"/> objects
        /// containing information about NTFS streams.</returns>
        public static NtfsInfo[] GetStreamsInfo(string path)
        {
            if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));
            if (-1 != path.IndexOfAny(Path.GetInvalidPathChars())) throw new ArgumentException(null, nameof(path));
            if (!File.Exists(path) && !Directory.Exists(path))
                throw new FileNotFoundException(null, path);

            var result = new List<NtfsInfo>();

            using (SafeFileHandle hFile = SafeCreateFile(path, NativeFileAccess.GenericRead, FileShare.Read,
                IntPtr.Zero, FileMode.Open, NativeFileFlags.BackupSemantics, IntPtr.Zero))
            //using (var hName = new NtfsFileStreamName())
            {
                if (!hFile.IsInvalid)
                {
                    var streamId = new Win32StreamId();
                    int dwStreamHeaderSize = Marshal.SizeOf(streamId);
                    bool finished = false;
                    IntPtr context = IntPtr.Zero;
                    int bytesRead;
                    string name;
                    SafeHGlobalHandle buffer = SafeHGlobalHandle.CreateInvalid();

                    try
                    {
                        while (!finished)
                        {
                            // Header of the next stream.
                            if (!BackupRead(hFile, ref streamId, dwStreamHeaderSize, out bytesRead, false, false, ref context))
                            {
                                finished = true;
                            }
                            else if (dwStreamHeaderSize != bytesRead)
                            {
                                finished = true;
                            }
                            else
                            {
                                // Stream name.
                                if (0 >= streamId.StreamNameSize)
                                {
                                    name = null;
                                }
                                else
                                {
                                    int currentSize = buffer.IsInvalid ? 0 : buffer.Size;
                                    int capacity = streamId.StreamNameSize;
                                    if (capacity > currentSize)
                                    {
                                        if (0 != currentSize) currentSize <<= 1;
                                        if (capacity > currentSize) currentSize = capacity;

                                        if (!buffer.IsInvalid) buffer.Dispose();
                                        buffer = SafeHGlobalHandle.Allocate(currentSize);
                                    }

                                    if (!BackupRead(hFile, buffer, streamId.StreamNameSize, out bytesRead, false, false, ref context))
                                    {
                                        name = null;
                                        finished = true;
                                    }
                                    else
                                    {
                                        // Unicode characters are 2 bytes.
                                        int length = bytesRead >> 1;
                                        if (0 >= length || buffer.IsInvalid) return null;
                                        if (length > buffer.Size) length = buffer.Size;
                                        name = Marshal.PtrToStringUni(buffer.DangerousGetHandle(), length);
                                        if (!string.IsNullOrEmpty(name))
                                        {
                                            // Name in the format ":NAME:$DATA\0"
                                            int separatorIndex = name.IndexOf(STREAM_SEP, 1);
                                            if (-1 != separatorIndex)
                                            {
                                                name = name.Substring(1, separatorIndex - 1);
                                            }
                                            else
                                            {
                                                separatorIndex = name.IndexOf('\0');
                                                if (1 < separatorIndex)
                                                {
                                                    name = name.Substring(1, separatorIndex - 1);
                                                }
                                                else
                                                {
                                                    name = null;
                                                }
                                            }
                                        }
                                    }
                                }

                                // Stream information.
                                if (!string.IsNullOrEmpty(name))
                                {
                                    result.Add(new NtfsInfo
                                    {
                                        StreamType = (NtfsType)streamId.StreamId,
                                        StreamAttributes = (NtfsAttributes)streamId.StreamAttributes,
                                        Size = streamId.Size.ToInt64(),
                                        Name = name
                                    });
                                }

                                // Skip the contents of the stream.
                                if (0 != streamId.Size.Low || 0 != streamId.Size.High)
                                {
                                    if (!finished && !BackupSeek(hFile, streamId.Size.Low, streamId.Size.High,
                                        out int low, out int high, ref context))
                                    {
                                        finished = true;
                                    }
                                }
                            }
                        }
                    }
                    finally
                    {
                        // Cancel backup.
                        BackupRead(hFile, buffer, 0, out bytesRead, true, false, ref context);
                        buffer?.Dispose();
                    }
                }
            }

            return result.ToArray();
        }

        /// <summary>
        /// Returns the current value of the transfer zone identifier for the specified file.
        /// </summary>
        /// <param name="filePath">
        /// The path to the file for which to retrieve the "transfer zone identifier" property.</param>
        /// <returns>A <see cref="ZoneId"/> value for the specified file.</returns>
        public static ZoneId GetZoneId(string filePath)
        {
            using (StreamReader reader = NtfsFileStream.OpenText(filePath, "Zone.Identifier"))
            {
                if (!reader.EndOfStream)
                    while (reader.ReadLine() != "[ZoneTransfer]")
                    {
                        if (reader.EndOfStream) return ZoneId.Undefined;
                    }
                while (reader.EndOfStream)
                {
                    string line = reader.ReadLine();
                    string[] pairing = line?.Split('=');
                    if (pairing?.Length == 2)
                    {
                        string key = pairing[0];
                        string value = pairing[1];
                        if (key == "ZoneId" && int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture,
                            out int id))
                            return (ZoneId)id;
                    }
                }
                return ZoneId.Undefined;
            }
        }

        /// <summary>
        /// Sets the transfer zone identifier for the specified file.
        /// </summary>
        /// <param name="filePath">
        /// The path to the file for which to set the "transfer zone identifier" property.</param>
        /// <param name="zoneId">
        /// The transfer zone identifier value to set for the specified file.</param>
        public static void SetZoneId(string filePath, ZoneId zoneId)
        {
            if (zoneId == ZoneId.Undefined)
            {
                // If the zoneId is undefined, delete the Zone.Identifier stream if it exists
                if (Exists(filePath, "Zone.Identifier"))
                {
                    Delete(filePath, "Zone.Identifier");
                }
            }
            else
            {
                using (Stream stream = NtfsFileStream.Create(filePath, "Zone.Identifier"))
                using (TextWriter writer = new StreamWriter(stream))
                {
                    writer.WriteLine("[ZoneTransfer]");
                    writer.WriteLine($"ZoneId={(int)zoneId}");
                }
            }
        }

        /// <summary>
        /// Removes the transfer zone identifier for the specified file.
        /// </summary>
        /// <param name="filePath">
        /// The path to the file for which to remove the "transfer zone identifier" property.
        /// </param>
        public static void RemoveZoneId(string filePath)
        {
            SetZoneId(filePath, ZoneId.Undefined);
        }

        #endregion

        #region SafeHGlobalHandle

        /// <summary>
        /// Represents a handle to a global memory block allocated using <see cref="Marshal.AllocHGlobal(int)"/>.
        /// </summary>
        private sealed class SafeHGlobalHandle : SafeHandle
        {
            private readonly int _size;

            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
            private SafeHGlobalHandle(IntPtr handle, int size) : base(IntPtr.Zero, true)
            {
                _size = size;
                SetHandle(handle);
            }

            private SafeHGlobalHandle() : base(IntPtr.Zero, true)
            {
            }

            /// <inheritdoc cref="SafeHandle.IsInvalid"/>
            public override bool IsInvalid => handle == IntPtr.Zero;

            /// <summary>
            /// The size of the allocated global memory block in bytes.
            /// </summary>
            public int Size => _size;

            /// <summary>
            /// Allocates memory from the unmanaged memory of the process, using the specified number of bytes.
            /// </summary>
            /// <param name="cb">The required number of bytes of memory.</param>
            /// <returns>
            /// A pointer to the newly allocated memory.
            /// This memory must be freed using <see cref="Marshal.FreeHGlobal(System.IntPtr)" />.
            /// </returns>
            public static SafeHGlobalHandle Allocate(int cb)
            {
                return new SafeHGlobalHandle(Marshal.AllocHGlobal(cb), cb);
            }

            /// <summary>
            /// Gets a handle initialized with the value of an invalid handle.
            /// </summary>
            /// <returns>The value of an invalid handle.</returns>
            public static SafeHGlobalHandle CreateInvalid()
            {
                return new SafeHGlobalHandle();
            }

            /// <inheritdoc cref="SafeHandle.ReleaseHandle"/>
            protected override bool ReleaseHandle()
            {
                Marshal.FreeHGlobal(handle);
                return true;
            }

        }

        #endregion

    }
}
