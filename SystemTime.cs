using System.Globalization;
using System.Runtime.InteropServices;
using System.Xml.Schema;
using System.Xml.Serialization;
using System.Xml;
using System;

/// <summary>
/// Represents a system time structure that provides access to system date and time information.
/// </summary>
[Serializable]
[StructLayout(LayoutKind.Explicit, Size = 16, CharSet = CharSet.Ansi)]
public sealed class SystemTime : object, IXmlSerializable, IFormattable, ICloneable, IEquatable<SystemTime>
{
    #region Fields

    // These fields represent the various components of a system time.

    [FieldOffset(0)] private ushort wYear;
    [FieldOffset(2)] private ushort wMonth;
    [FieldOffset(4)] private ushort wDayOfWeek;
    [FieldOffset(6)] private ushort wDay;
    [FieldOffset(8)] private ushort wHour;
    [FieldOffset(10)] private ushort wMinute;
    [FieldOffset(12)] private ushort wSecond;
    [FieldOffset(14)] private ushort wMilliseconds;

    #endregion

    #region Properties

    /// <summary>
    /// Gets or sets the year.
    /// </summary>
    public ushort Year
    {
        // Check if the year is within the valid range
        get => wYear;
        set
        {
            wYear = value > 1600 && value < 30828
                ? value
                : throw new ArgumentOutOfRangeException(nameof(Year), value, null);
            int daysInMonth = GetDaysInMonth(wMonth, wYear);
            if (wDay > daysInMonth) wDay = (ushort)daysInMonth;
            wDayOfWeek = (ushort)GetDayOfWeek(wDay, wMonth, wYear);
        }
    }

    /// <summary>
    /// Gets or sets the month.
    /// </summary>
    public ushort Month
    {
        // Check if the month is within the valid range
        get => wMonth;
        set
        {
            wMonth = value > 0 && value < 13
                ? value
                : throw new ArgumentOutOfRangeException(nameof(Month), value, null);
            int daysInMonth = GetDaysInMonth(wMonth, wYear);
            if (wDay > daysInMonth) wDay = (ushort)daysInMonth;
            wDayOfWeek = (ushort)GetDayOfWeek(wDay, wMonth, wYear);
        }
    }

    /// <summary>
    /// Gets or sets the day.
    /// </summary>
    public ushort Day
    {
        // Check if the day is within the valid range
        get => wDay;
        set
        {
            wDay = value > 0 && value <= GetDaysInMonth(wMonth, wYear)
                ? value
                : throw new ArgumentOutOfRangeException(nameof(Day), value, null);
            wDayOfWeek = (ushort)GetDayOfWeek(wDay, wMonth, wYear);
        }
    }

    /// <summary>
    /// Gets the day of the week.
    /// </summary>
    public DayOfWeek DayOfWeek
    {
        get => (DayOfWeek)wDayOfWeek;
    }

    /// <summary>
    /// Gets or sets the hour.
    /// </summary>
    public ushort Hour
    {
        // Check if the hour is within the valid range
        get => wHour;
        set => wHour = value < 24 ? value
            : throw new ArgumentOutOfRangeException(nameof(Hour), value, null);
    }

    /// <summary>
    /// Gets or sets the minute.
    /// </summary>
    public ushort Minute
    {
        // Check if the minute is within the valid range
        get => wMinute;
        set => wMinute = value < 60 ? value
            : throw new ArgumentOutOfRangeException(nameof(Minute), value, null);
    }

    /// <summary>
    /// Gets or sets the second.
    /// </summary>
    public ushort Second
    {
        // Check if the second is within the valid range
        get => wSecond;
        set => wSecond = value < 60 ? value
            : throw new ArgumentOutOfRangeException(nameof(Second), value, null);
    }

    /// <summary>
    /// Gets or sets the milliseconds.
    /// </summary>
    public ushort Milliseconds
    {
        // Check if the milliseconds are within the valid range
        get => wMilliseconds;
        set => wMilliseconds = value < 1000 ? value
            : throw new ArgumentOutOfRangeException(nameof(Milliseconds), value, null);
    }

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="SystemTime"/> class.
    /// </summary>
    public SystemTime()
    {
        // Initialize the system time to January 1, 1601, 00:00:00.000
        wYear = 1601;
        wMonth = 1;
        wDay = 1;
        wDayOfWeek = (ushort)GetDayOfWeek(wDay, wMonth, wYear);
        wHour = 0;
        wMinute = 0;
        wSecond = 0;
        wMilliseconds = 0;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SystemTime"/> class from a file time.
    /// </summary>
    /// <param name="fileTime">The file time.</param>
    public SystemTime(long fileTime)
    {
        FileTimeToSystemTimeNative(fileTime, this);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SystemTime"/> class from a <see cref="DateTime"/>.
    /// </summary>
    /// <param name="dateTime">The date and time.</param>
    public SystemTime(DateTime dateTime)
    {
        long fileTime = dateTime.ToFileTimeUtc();
        SystemTime systemTime = new SystemTime();
        FileTimeToSystemTimeNative(fileTime, systemTime);
    }

    #endregion

    #region Win32 imports

    [DllImport("kernel32.dll", EntryPoint = "GetSystemTime", SetLastError = true)]
    private static extern void GetSystemTimeNative([Out, MarshalAs(UnmanagedType.Struct)]
    SystemTime systemTime);

    [DllImport("kernel32.dll", EntryPoint = "SetLocalTime", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetLocalTimeNative([In, MarshalAs(UnmanagedType.Struct)]
    SystemTime systemTime);

    [DllImport("kernel32.dll", EntryPoint = "SetSystemTime", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetSystemTimeNative([In, MarshalAs(UnmanagedType.Struct)]
    SystemTime systemTime);

    [DllImport("kernel32.dll", EntryPoint = "GetLocalTime", SetLastError = true)]
    private static extern void GetLocalTimeNative([Out, MarshalAs(UnmanagedType.Struct)]
    SystemTime systemTime);

    [DllImport("kernel32.dll", EntryPoint = "FileTimeToSystemTime", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool FileTimeToSystemTimeNative([In, MarshalAs(UnmanagedType.I8)] long fileTime,
    [Out, MarshalAs(UnmanagedType.Struct)]
    SystemTime systemTime);

    [DllImport("kernel32.dll", EntryPoint = "SystemTimeToFileTime", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SystemTimeToFileTimeNative([In, MarshalAs(UnmanagedType.Struct)]
    SystemTime systemTime,
    [Out, MarshalAs(UnmanagedType.I8)] out long fileTime);

    [DllImport("kernel32.dll", EntryPoint = "GetTickCount", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.U4)]
    private static extern ulong GetTickCountNative();

    [DllImport("kernel32.dll", EntryPoint = "GetTickCount64", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.U8)]
    private static extern ulong GetTickCount64Native();

    [DllImport("kernel32.dll", EntryPoint = "GetSystemTimeAdjustment", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetSystemTimeAdjustmentNative(
    [Out, MarshalAs(UnmanagedType.U4)] out ulong lpTimeAdjustment,
    [Out, MarshalAs(UnmanagedType.U4)] out ulong lpTimeIncrement,
    [Out, MarshalAs(UnmanagedType.Bool)] out bool lpTimeAdjustmentDisabled);

    [DllImport("kernel32.dll", EntryPoint = "SetSystemTimeAdjustment", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetSystemTimeAdjustmentNative(
    [In, MarshalAs(UnmanagedType.U4)] ulong dwTimeAdjustment,
    [In, MarshalAs(UnmanagedType.Bool)] bool bTimeAdjustmentDisabled);

    #endregion

    #region Methods

    // Checks if the specified year is a leap.
    private static bool CheckIsLeapYear(int year)
    {
        return year % 4 == 0 && year % 100 != 0 || year % 400 == 0;
    }

    // Gets the number of days in the specified month and year.
    private static int GetDaysInMonth(int month, int year)
    {
        return month == 2 && (year % 4 == 0 && year % 100 != 0 || year % 400 == 0) ? 29 : 28 + ((62648012 >> (month * 2)) & 3);
    }

    // Calculates the day of the week for the given day, month, and year.
    private static int GetDayOfWeek(int day, int month, int year)
    {
        int a = (14 - month) / 12;
        int y = year - a;
        int m = month + 12 * a - 2;
        return ((7000 + (day + y + y / 4 - y / 100 + y / 400 + (31 * m) / 12)) % 7);
    }

    /// <summary>
    /// Converts the current <see cref="SystemTime"/> instance to a file time.
    /// </summary>
    /// <returns>The file time representation of the current <see cref="SystemTime"/> instance.</returns>
    public long ToFileTime()
    {
        return SystemTimeToFileTimeNative(this, out long fileTime) ? fileTime : 0L;
    }

    /// <summary>
    /// Converts the current <see cref="SystemTime"/> instance to a <see cref="DateTime"/> object.
    /// </summary>
    /// <returns>The <see cref="DateTime"/> representation of the current <see cref="SystemTime"/> instance.</returns>
    public DateTime ToDateTime()
    {
        return SystemTimeToFileTimeNative(this, out long fileTime)
            ? DateTime.FromFileTimeUtc(fileTime)
            : DateTime.MinValue;
    }

    /// <summary>
    /// Converts the current <see cref="SystemTime"/> instance to a Unix timestamp.
    /// </summary>
    /// <returns>The Unix timestamp representation of the current <see cref="SystemTime"/> instance.</returns>
    public double ToUnixTime()
    {
        SystemTimeToFileTimeNative(this, out long fileTime);
        long seconds = fileTime / 10000000;
        double unixTime = seconds - 11644473600;
        return Math.Round(unixTime, 2);
    }

    /// <summary>
    /// Creates a new <see cref="SystemTime"/> instance from a file time.
    /// </summary>
    /// <param name="fileTime">The file time to convert to a system time.</param>
    /// <returns>A new <see cref="SystemTime"/> instance representing the specified file time.</returns>
    public static SystemTime FromFileTime(long fileTime)
    {
        SystemTime systemTime = new SystemTime();
        FileTimeToSystemTimeNative(fileTime, systemTime);
        return systemTime;
    }

    /// <summary>
    /// Creates a new <see cref="SystemTime"/> instance from a <see cref="DateTime"/> object.
    /// </summary>
    /// <param name="dateTime">The date and time to convert to a system time.</param>
    /// <returns>A new <see cref="SystemTime"/> instance representing the specified date and time.</returns>
    public static SystemTime FromDateTime(DateTime dateTime)
    {
        long fileTime = dateTime.ToFileTimeUtc();
        SystemTime systemTime = new SystemTime();
        FileTimeToSystemTimeNative(fileTime, systemTime);
        return systemTime;
    }

    /// <summary>
    /// Creates a new <see cref="SystemTime"/> instance from a Unix timestamp.
    /// </summary>
    /// <param name="unixTime">The Unix timestamp to convert to a system time.</param>
    /// <returns>A new <see cref="SystemTime"/> instance representing the specified Unix timestamp.</returns>
    public static SystemTime FromUnixTime(double unixTime)
    {
        SystemTime systemTime = new SystemTime();
        long seconds = Convert.ToInt64(Math.Floor(unixTime + 11644473600));
        long fileTime = seconds * 10000000;
        FileTimeToSystemTimeNative(fileTime, systemTime);
        return systemTime;
    }

    /// <summary>
    /// Sets the local system time to the specified <see cref="SystemTime"/> instance.
    /// </summary>
    /// <param name="systemTime">The system time to set as the local system time.</param>
    /// <returns>True if the local system time was successfully set; otherwise, false.</returns>
    public static bool SetLocalTime(SystemTime systemTime)
    {
        return systemTime != null && SetLocalTimeNative(systemTime);
    }

    /// <summary>
    /// Sets the local system time to the specified <see cref="DateTime"/> instance.
    /// </summary>
    /// <param name="dateTime">The date and time to set as the local system time.</param>
    /// <returns>True if the local system time was successfully set; otherwise, false.</returns>
    public static bool SetLocalTime(DateTime dateTime)
    {
        var systemTime = FromDateTime(dateTime);
        return systemTime != null && SetLocalTimeNative(systemTime);
    }

    /// <summary>
    /// Sets the system time to the specified <see cref="SystemTime"/> instance.
    /// </summary>
    /// <param name="systemTime">The system time to set as the system time.</param>
    /// <returns>True if the system time was successfully set; otherwise, false.</returns>
    public static bool SetSystemTime(SystemTime systemTime)
    {
        return systemTime != null && SetSystemTimeNative(systemTime);
    }

    /// <summary>
    /// Sets the system time to the specified <see cref="DateTime"/> instance.
    /// </summary>
    /// <param name="dateTime">The date and time to set as the system time.</param>
    /// <returns>True if the system time was successfully set; otherwise, false.</returns>
    public static bool SetSystemTime(DateTime dateTime)
    {
        var systemTime = FromDateTime(dateTime);
        return systemTime != null && SetSystemTimeNative(systemTime);
    }

    /// <summary>
    /// Gets the current system time.
    /// </summary>
    /// <returns>A <see cref="SystemTime"/> instance representing the current system time.</returns>
    public static SystemTime GetSystemTime()
    {
        SystemTime systemTime = new SystemTime();
        GetSystemTimeNative(systemTime);
        return systemTime;
    }

    /// <summary>
    /// Gets the current local time.
    /// </summary>
    /// <returns>A <see cref="SystemTime"/> instance representing the current local time.</returns>
    public static SystemTime GetLocalTime()
    {
        SystemTime systemTime = new SystemTime();
        GetLocalTimeNative(systemTime);
        return systemTime;
    }

    /// <inheritdoc />
    public XmlSchema GetSchema()
    {
        return null;
    }

    /// <inheritdoc />
    public void ReadXml(XmlReader reader)
    {
        long fileTime = reader.ReadContentAsLong();
        FileTimeToSystemTimeNative(fileTime, this);
    }

    /// <inheritdoc />
    public void WriteXml(XmlWriter writer)
    {
        SystemTimeToFileTimeNative(this, out long fileTime);
        writer.WriteValue(fileTime);
    }

    /// <inheritdoc />
    public override bool Equals(object obj)
    {
        return ReferenceEquals(this, obj)
               || obj is SystemTime systemTime && this == systemTime
               || obj is long fileTime && ToFileTime() == fileTime
               || obj is DateTime dt && ToDateTime() == dt;
    }

    /// <summary>
    /// Returns the hash code for this instance.
    /// </summary>
    /// <returns>
    /// A 32-bit signed integer hash code.
    /// </returns>
    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = wYear.GetHashCode();
            hashCode = (hashCode * 397) ^ wMonth.GetHashCode();
            hashCode = (hashCode * 397) ^ wDayOfWeek.GetHashCode();
            hashCode = (hashCode * 397) ^ wDay.GetHashCode();
            hashCode = (hashCode * 397) ^ wHour.GetHashCode();
            hashCode = (hashCode * 397) ^ wMinute.GetHashCode();
            hashCode = (hashCode * 397) ^ wSecond.GetHashCode();
            hashCode = (hashCode * 397) ^ wMilliseconds.GetHashCode();
            return hashCode;
        }
    }

    /// <summary>
    /// Indicates whether the current <see cref="SystemTime"/> object
    /// is equal to another object of the same type.
    /// </summary>
    /// <param name="other">The object to be compared with this object.</param>
    /// <returns>
    /// <see langword="true" /> if the current object is equivalent to <paramref name="other" />,
    /// otherwise <see langword="false" />.
    /// </returns>
    public bool Equals(SystemTime other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return wYear == other.wYear
               && wMonth == other.wMonth
               && wDayOfWeek == other.wDayOfWeek
               && wDay == other.wDay
               && wHour == other.wHour
               && wMinute == other.wMinute
               && wSecond == other.wSecond
               && wMilliseconds == other.wMilliseconds;
    }

    /// <summary>
    /// Creates a deep copy of the current <see cref="SystemTime"/> object.
    /// </summary>
    /// <returns>
    /// A new <see cref="SystemTime"/> object with the same values as the current object.
    /// </returns>
    public object Clone()
    {
        // Create a new SystemTime object
        SystemTime clone = new SystemTime
        {
            // Copy the values of the current object to the new object
            wYear = this.wYear,
            wMonth = this.wMonth,
            wDayOfWeek = this.wDayOfWeek,
            wDay = this.wDay,
            wHour = this.wHour,
            wMinute = this.wMinute,
            wSecond = this.wSecond,
            wMilliseconds = this.wMilliseconds
        };

        return clone;
    }

    /// <summary>
    /// Converts the value of the current <see cref="SystemTime" /> object
    /// to its equivalent string representation using the formatting conventions of the current culture.
    /// </summary>
    /// <returns>
    /// A string representation of the value of the current <see cref="SystemTime" /> object.
    /// </returns>
    public override string ToString()
    {
        return ToDateTime().ToString(DateTimeFormatInfo.CurrentInfo);
    }

    /// <summary>
    /// Converts the value of the current <see cref="SystemTime" /> object
    /// to its equivalent string representation using the specified culture-specific format information.
    /// </summary>
    /// <param name="provider">An object that supplies culture-specific formatting information.</param>
    /// <returns>
    /// A string representation of value of the current <see cref="SystemTime" /> object
    /// as specified by <paramref name="provider" />.
    /// </returns>
    public string ToString(IFormatProvider provider)
    {
        return ToDateTime().ToString(provider);
    }

    /// <summary>
    /// Converts the value of the current <see cref="SystemTime" />
    /// object to its equivalent string representation using the specified
    /// format and the formatting conventions of the current culture.
    /// </summary>
    /// <param name="format">A standard or custom date and time format string. </param>
    /// <returns>
    /// A string representation of value of the current <see cref="SystemTime" /> object
    /// as specified by <paramref name="format" />.
    /// </returns>
    public string ToString(string format)
    {
        return ToDateTime().ToString(format);
    }

    /// <summary>
    /// Converts the value of the current <see cref="SystemTime" /> object
    /// to its equivalent string representation using the specified format
    /// and culture-specific format information.
    /// </summary>
    /// <param name="format">A standard or custom date and time format string.</param>
    /// <param name="provider">An object that supplies culture-specific formatting information.</param>
    /// <returns>
    /// A string representation of value of the current <see cref="SystemTime" /> object
    /// as specified by <paramref name="format" /> and <paramref name="provider" />.
    /// </returns>
    public string ToString(string format, IFormatProvider provider)
    {
        return ToDateTime().ToString(format, provider);
    }

    #endregion

    #region Operators

    /// <summary>
    /// Converts a <see cref="SystemTime"/> instance to a <see cref="DateTime"/> instance.
    /// </summary>
    /// <param name="systemTime">The <see cref="SystemTime"/> instance to convert.</param>
    public static explicit operator DateTime(SystemTime systemTime)
    {
        return systemTime.ToDateTime();
    }

    /// <summary>
    /// Converts a <see cref="DateTime"/> instance to a <see cref="SystemTime"/> instance.
    /// </summary>
    /// <param name="dateTime">The <see cref="DateTime"/> instance to convert.</param>
    public static explicit operator SystemTime(DateTime dateTime)
    {
        return FromDateTime(dateTime);
    }

    /// <summary>
    /// Converts a <see cref="SystemTime"/> instance to a <see cref="long"/> file time value.
    /// </summary>
    /// <param name="systemTime">The <see cref="SystemTime"/> instance to convert.</param>
    public static explicit operator long(SystemTime systemTime)
    {
        return systemTime.ToFileTime();
    }

    /// <summary>
    /// Converts a <see cref="long"/> file time value to a <see cref="SystemTime"/> instance.
    /// </summary>
    /// <param name="fileTime">The file time value to convert.</param>
    public static explicit operator SystemTime(long fileTime)
    {
        return FromFileTime(fileTime);
    }

    /// <summary>
    /// Converts a <see cref="SystemTime"/> instance to a <see cref="double"/> Unix time value.
    /// </summary>
    /// <param name="systemTime">The <see cref="SystemTime"/> instance to convert.</param>
    public static explicit operator double(SystemTime systemTime)
    {
        return systemTime.ToUnixTime();
    }

    /// <summary>
    /// Converts a <see cref="double"/> Unix time value to a <see cref="SystemTime"/> instance.
    /// </summary>
    /// <param name="unixTime">The Unix time value to convert.</param>
    public static explicit operator SystemTime(double unixTime)
    {
        return FromUnixTime(unixTime);
    }

    /// <summary>
    /// Adds a time interval to a <see cref="SystemTime"/> instance.
    /// </summary>
    /// <param name="origin">The <see cref="SystemTime"/> instance to add the interval to.</param>
    /// <param name="interval">The time interval to add.</param>
    /// <returns>A new <see cref="SystemTime"/> instance representing the original time plus the interval.</returns>
    public static SystemTime operator +(SystemTime origin, long interval)
    {
        return FromFileTime(origin.ToFileTime() + interval);
    }

    /// <summary>
    /// Adds a <see cref="TimeSpan"/> to a <see cref="SystemTime"/> instance.
    /// </summary>
    /// <param name="origin">The <see cref="SystemTime"/> instance to add the interval to.</param>
    /// <param name="interval">The <see cref="TimeSpan"/> to add.</param>
    /// <returns>A new <see cref="SystemTime"/> instance representing the original time plus the interval.</returns>
    public static SystemTime operator +(SystemTime origin, TimeSpan interval)
    {
        return FromDateTime(origin.ToDateTime() + interval);
    }

    /// <summary>
    /// Subtracts one <see cref="SystemTime"/> instance from another, returning the time interval between them.
    /// </summary>
    /// <param name="left">The <see cref="SystemTime"/> instance to subtract from.</param>
    /// <param name="right">The <see cref="SystemTime"/> instance to subtract.</param>
    /// <returns>The time interval between the two <see cref="SystemTime"/> instances.</returns>
    public static long operator -(SystemTime left, SystemTime right)
    {
        return left.ToFileTime() - right.ToFileTime();
    }

    /// <summary>
    /// Subtracts a time interval from a <see cref="SystemTime"/> instance.
    /// </summary>
    /// <param name="origin">The <see cref="SystemTime"/> instance to subtract the interval from.</param>
    /// <param name="interval">The time interval to subtract.</param>
    /// <returns>A new <see cref="SystemTime"/> instance representing the original time minus the interval.</returns>
    public static SystemTime operator -(SystemTime origin, long interval)
    {
        return FromFileTime(origin.ToFileTime() - interval);
    }

    /// <summary>
    /// Subtracts a <see cref="TimeSpan"/> from a <see cref="SystemTime"/> instance.
    /// </summary>
    /// <param name="origin">The <see cref="SystemTime"/> instance to subtract the interval from.</param>
    /// <param name="interval">The <see cref="TimeSpan"/> to subtract.</param>
    /// <returns>A new <see cref="SystemTime"/> instance representing the original time minus the interval.</returns>
    public static SystemTime operator -(SystemTime origin, TimeSpan interval)
    {
        return FromDateTime(origin.ToDateTime() - interval);
    }

    /// <summary>
    /// Compares two <see cref="SystemTime"/> instances to determine if the left operand is greater than the right operand.
    /// </summary>
    /// <param name="left">The left <see cref="SystemTime"/> instance to compare.</param>
    /// <param name="right">The right <see cref="SystemTime"/> instance to compare.</param>
    /// <returns>True if the left operand is greater than the right operand, false otherwise.</returns>
    public static bool operator >(SystemTime left, SystemTime right)
    {
        return left.ToFileTime() > right.ToFileTime();
    }

    /// <summary>
    /// Compares two <see cref="SystemTime"/> instances to determine if the left operand is less than the right operand.
    /// </summary>
    /// <param name="left">The left <see cref="SystemTime"/> instance to compare.</param>
    /// <param name="right">The right <see cref="SystemTime"/> instance to compare.</param>
    /// <returns>True if the left operand is less than the right operand, false otherwise.</returns>
    public static bool operator <(SystemTime left, SystemTime right)
    {
        return left.ToFileTime() < right.ToFileTime();
    }

    /// <summary>
    /// Compares two <see cref="SystemTime"/> instances to determine if the left operand is greater than or equal to the right operand.
    /// </summary>
    /// <param name="left">The left <see cref="SystemTime"/> instance to compare.</param>
    /// <param name="right">The right <see cref="SystemTime"/> instance to compare.</param>
    /// <returns>True if the left operand is greater than or equal to the right operand, false otherwise.</returns>
    public static bool operator >=(SystemTime left, SystemTime right)
    {
        return left.ToFileTime() >= right.ToFileTime();
    }

    /// <summary>
    /// Compares two <see cref="SystemTime"/> instances to determine if the left operand is less than or equal to the right operand.
    /// </summary>
    /// <param name="left">The left <see cref="SystemTime"/> instance to compare.</param>
    /// <param name="right">The right <see cref="SystemTime"/> instance to compare.</param>
    /// <returns>True if the left operand is less than or equal to the right operand, false otherwise.</returns>
    public static bool operator <=(SystemTime left, SystemTime right)
    {
        return left.ToFileTime() <= right.ToFileTime();
    }

    /// <summary>
    /// Compares two <see cref="SystemTime"/> instances for equality.
    /// </summary>
    /// <param name="left">The left <see cref="SystemTime"/> instance to compare.</param>
    /// <param name="right">The right <see cref="SystemTime"/> instance to compare.</param>
    /// <returns>True if the two <see cref="SystemTime"/> instances are equal, false otherwise.</returns>
    public static bool operator ==(SystemTime left, SystemTime right)
    {
        return left == null && right == null || left?.ToFileTime() == right?.ToFileTime();
    }

    /// <summary>
    /// Compares two <see cref="SystemTime"/> instances for inequality.
    /// </summary>
    /// <param name="left">The left <see cref="SystemTime"/> instance to compare.</param>
    /// <param name="right">The right <see cref="SystemTime"/> instance to compare.</param>
    /// <returns>True if the two <see cref="SystemTime"/> instances are not equal, false otherwise.</returns>
    public static bool operator !=(SystemTime left, SystemTime right)
    {
        return left == null ^ right == null || left?.ToFileTime() != right?.ToFileTime();
    }


    #endregion
}
