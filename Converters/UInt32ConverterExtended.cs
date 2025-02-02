/*************************************************************** 

•   File: UInt32ConverterExtended.cs

•   Description

   The  UInt32ConverterExtended class extends the functionality
    of  the  BaseNumberConverterExtended    class   to   provide
    specialized   conversion  methods   for  the unsigned 32-bit
    integer (UInt32) type. It  supports conversions  from string
    representations    in   various  number   systems  (decimal,
    hexadecimal, binary, octal) and    allows the  conversion of
    UInt32   values  to    strings    using  a    custom format.

•   Copyright

    © Pavel Bashkardin, 2022-2024

***************************************************************/

using System.Globalization;
using System.Security.Permissions;

namespace System.ComponentModel
{
    /// <summary>
    ///     The <see cref="UInt32ConverterExtended"/> class extends the functionality 
    ///     of the <see cref="NumberConverterExtended"/> class for converting 
    ///     UInt32 (unsigned 32-bit integer) values. It supports parsing strings with different 
    ///     number system prefixes (hexadecimal, binary, octal, etc.) and converting 
    ///     UInt32 values to their string representations using custom formatting.
    /// </summary>
    [HostProtection(SecurityAction.LinkDemand, SharedState = true)]
    public class UInt32ConverterExtended : NumberConverterExtended
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="UInt32ConverterExtended"/> class.
        /// </summary>
        public UInt32ConverterExtended() : base(typeof(uint)) { }

        /// <summary>
        ///     Converts a string representation of a number to a <see cref="uint"/> 
        ///     value using the specified radix.
        /// </summary>
        /// <param name="value">
        ///     The string representation of the number.
        /// </param>
        /// <param name="radix">
        ///     The base (radix) to use for conversion (e.g., 10 for decimal, 16 for hexadecimal).
        /// </param>
        /// <returns>
        ///     The converted <see cref="uint"/> value.
        /// </returns>
        protected override object ConvertFromString(string value, int radix)
        {
            return Convert.ToUInt32(value, radix);
        }

        /// <summary>
        ///     Converts a string representation of a number to a <see cref="uint"/> 
        ///     value using the specified culture information.
        /// </summary>
        /// <param name="value">
        ///     The string representation of the number.
        /// </param>
        /// <param name="culture">
        ///     The <see cref="CultureInfo"/> used for parsing.
        /// </param>
        /// <returns>
        ///     The converted <see cref="uint"/> value.
        /// </returns>
        protected override object ConvertFromString(string value, CultureInfo culture)
        {
            return uint.Parse(value, culture);
        }

        /// <summary>
        ///     Converts a <see cref="uint"/> value to its string representation 
        ///     using the specified number format information.
        /// </summary>
        /// <param name="value">
        ///     The <see cref="uint"/> value.
        /// </param>
        /// <param name="culture">
        ///     The <see cref="CultureInfo"/> used for conversion.
        /// </param>
        /// <returns>
        ///     The string representation of the <see cref="uint"/> value.
        /// </returns>
        protected override string ConvertToString(object value, CultureInfo culture)
        {
            return ((uint)value).ToString("G", culture);
        }
    }
}
