/*************************************************************** 

•   File: SingleConverterExtended.cs

•   Description

    The  SingleConverterExtended class extends the functionality
    of  the  BaseNumberConverterExtended    class   to   provide
    specialized   conversion  methods   for the single-precision
    floating point (Single) type. It  does not  support  parsing
    strings with different   number system prefixes. Instead, it
    focuses  on  decimal values  and converting Single values to
    strings using custom formatting.


•   Copyright

    © Pavel Bashkardin, 2022-2024

***************************************************************/

using System.Globalization;
using System.Security.Permissions;

namespace System.ComponentModel
{
    /// <summary>
    ///     The <see cref="SingleConverterExtended"/> class extends the functionality 
    ///     of the <see cref="NumberConverterExtended"/> class for converting 
    ///     Single (single-precision floating point) values. It supports parsing strings 
    ///     with different number system prefixes (e.g., decimal) and converting 
    ///     Single values to their string representations using custom formatting.
    /// </summary>
    [HostProtection(SecurityAction.LinkDemand, SharedState = true)]
    public class SingleConverterExtended : NumberConverterExtended
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="SingleConverterExtended"/> class.
        /// </summary>
        public SingleConverterExtended() : base(allowBaseEncoding: false, targetType: typeof(float)) { }

        /// <summary>
        ///     Converts a string representation of a number to a <see cref="float"/> 
        ///     value using the specified radix.
        /// </summary>
        /// <param name="value">
        ///     The string representation of the number.
        /// </param>
        /// <param name="radix">
        ///     The base (radix) to use for conversion is unsupported.
        /// </param>
        /// <returns>
        ///     The converted <see cref="float"/> value.
        /// </returns>
        [Obsolete]
        protected override object ConvertFromString(string value, int radix)
        {
            return Convert.ToSingle(value, CultureInfo.CurrentCulture);
        }

        /// <summary>
        ///     Converts a string representation of a number to a <see cref="float"/> 
        ///     value using the specified culture information.
        /// </summary>
        /// <param name="value">
        ///     The string representation of the number.
        /// </param>
        /// <param name="culture">
        ///     The <see cref="CultureInfo"/> used for parsing.
        /// </param>
        /// <returns>
        ///     The converted <see cref="float"/> value.
        /// </returns>
        protected override object ConvertFromString(string value, CultureInfo culture)
        {
            return float.Parse(value, culture);
        }

        /// <summary>
        ///     Converts a <see cref="float"/> value to its string representation 
        ///     using the specified number format information.
        /// </summary>
        /// <param name="value">
        ///     The <see cref="float"/> value.
        /// </param>
        /// <param name="culture">
        ///     The <see cref="CultureInfo"/> used for conversion.
        /// </param>
        /// <returns>
        ///     The string representation of the <see cref="float"/> value.
        /// </returns>
        protected override string ConvertToString(object value, CultureInfo culture)
        {
            return ((float)value).ToString("R", culture);
        }
    }
}
