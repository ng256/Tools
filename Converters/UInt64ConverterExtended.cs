/*************************************************************** 

•   File: UInt64ConverterExtended.cs

•   Description

    The  UInt64ConverterExtended class extends the functionality
    of  the  BaseNumberConverterExtended    class   to   provide
    specialized   conversion  methods   for  the unsigned 64-bit
    integer (UInt64) type. It  supports conversions  from string
    representations    in   various  number   systems  (decimal,
    hexadecimal, binary, octal) and    allows the  conversion of
    UInt64   values  to    strings    using  a    custom format.

•   Copyright

    © Pavel Bashkardin, 2022-2024

***************************************************************/

using System.Globalization;
using System.Security.Permissions;

namespace System.ComponentModel
{
    /// <summary>
    ///     The <see cref="UInt64ConverterExtended"/> class extends the functionality 
    ///     of the <see cref="NumberConverterExtended"/> class for converting 
    ///     UInt64 (unsigned 64-bit integer) values. It supports parsing strings with different 
    ///     number system prefixes (hexadecimal, binary, octal, etc.) and converting 
    ///     UInt64 values to their string representations using custom formatting.
    /// </summary>
    [HostProtection(SecurityAction.LinkDemand, SharedState = true)]
    public class UInt64ConverterExtended : NumberConverterExtended
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="UInt64ConverterExtended"/> class.
        /// </summary>
        public UInt64ConverterExtended() : base(typeof(ulong)) { }

        /// <summary>
        ///     Converts a string representation of a number to a <see cref="ulong"/> 
        ///     value using the specified radix.
        /// </summary>
        /// <param name="value">
        ///     The string representation of the number.
        /// </param>
        /// <param name="radix">
        ///     The base (radix) to use for conversion (e.g., 10 for decimal, 16 for hexadecimal).
        /// </param>
        /// <returns>
        ///     The converted <see cref="ulong"/> value.
        /// </returns>
        protected override object ConvertFromString(string value, int radix)
        {
            return Convert.ToUInt64(value, radix);
        }

        /// <summary>
        ///     Converts a string representation of a number to a <see cref="ulong"/> 
        ///     value using the specified culture information.
        /// </summary>
        /// <param name="value">
        ///     The string representation of the number.
        /// </param>
        /// <param name="culture">
        ///     The <see cref="CultureInfo"/> used for parsing.
        /// </param>
        /// <returns>
        ///     The converted <see cref="ulong"/> value.
        /// </returns>
        protected override object ConvertFromString(string value, CultureInfo culture)
        {
            return ulong.Parse(value, culture);
        }

        /// <summary>
        ///     Converts a <see cref="ulong"/> value to its string representation 
        ///     using the specified number format information.
        /// </summary>
        /// <param name="value">
        ///     The <see cref="ulong"/> value.
        /// </param>
        /// <param name="culture">
        ///     The <see cref="CultureInfo"/> used for conversion.
        /// </param>
        /// <returns>
        ///     The string representation of the <see cref="ulong"/> value.
        /// </returns>
        protected override string ConvertToString(object value, CultureInfo culture)
        {
            return ((ulong)value).ToString("G", culture);
        }
    }
}
