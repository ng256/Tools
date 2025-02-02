/*************************************************************** 

•   File: Int32ConverterExtended.cs

•   Description

    The  Int32ConverterExtended class  extends the functionality
    of  the  BaseNumberConverterExtended    class   to   provide
    specialized conversion methods for the signed 32-bit integer
    (Int32)    type.  It   supports    conversions  from  string
    representations   in  various   number  systems    (decimal,
    hexadecimal,  binary, octal) and  allows  the  conversion of
    Int32   values  to     strings    using  a    custom format.

•   Copyright

    © Pavel Bashkardin, 2022-2024

***************************************************************/

using System.Globalization;
using System.Security.Permissions;

namespace System.ComponentModel
{
    /// <summary>
    ///     The <see cref="Int32ConverterExtended"/> class extends the functionality 
    ///     of the <see cref="NumberConverterExtended"/> class for converting 
    ///     Int32 (signed 32-bit integer) values. It supports parsing strings with different 
    ///     number system prefixes (hexadecimal, binary, octal, etc.) and converting 
    ///     Int32 values to their string representations using custom formatting.
    /// </summary>
    [HostProtection(SecurityAction.LinkDemand, SharedState = true)]
    public class Int32ConverterExtended : NumberConverterExtended
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="Int32ConverterExtended"/> class.
        /// </summary>
        public Int32ConverterExtended() : base(typeof(int)) { }

        /// <summary>
        ///     Converts a string representation of a number to an <see cref="int"/> 
        ///     value using the specified radix.
        /// </summary>
        /// <param name="value">
        ///     The string representation of the number.
        /// </param>
        /// <param name="radix">
        ///     The base (radix) to use for conversion (e.g., 10 for decimal, 16 for hexadecimal).
        /// </param>
        /// <returns>
        ///     The converted <see cref="int"/> value.
        /// </returns>
        protected override object ConvertFromString(string value, int radix)
        {
            return Convert.ToInt32(value, radix);
        }

        /// <summary>
        ///     Converts a string representation of a number to an <see cref="int"/> 
        ///     value using the specified culture information.
        /// </summary>
        /// <param name="value">
        ///     The string representation of the number.
        /// </param>
        /// <param name="culture">
        ///     The <see cref="CultureInfo"/> used for parsing.
        /// </param>
        /// <returns>
        ///     The converted <see cref="int"/> value.
        /// </returns>
        protected override object ConvertFromString(string value, CultureInfo culture)
        {
            return int.Parse(value, culture);
        }

        /// <summary>
        ///     Converts an <see cref="int"/> value to its string representation 
        ///     using the specified number format information.
        /// </summary>
        /// <param name="value">
        ///     The <see cref="int"/> value.
        /// </param>
        /// <param name="culture">
        ///     The <see cref="CultureInfo"/> used for conversion.
        /// </param>
        /// <returns>
        ///     The string representation of the <see cref="int"/> value.
        /// </returns>
        protected override string ConvertToString(object value, CultureInfo culture)
        {
            return ((int)value).ToString("G", culture);
        }
    }
}
