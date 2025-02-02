/*************************************************************** 

•   File: DecimalConverterExtended.cs

•   Description

    The  DecimalConverterExtended class extends the functionality
    of  the  BaseNumberConverterExtended    class   to    provide
    specialized   conversion  methods   for the 128 bit-precision
    floating point (Decimal) type. It  does not  support  parsing
    strings with different   number system prefixes.  Instead, it
    focuses  on  decimal values  and converting Decimal values to
    strings using custom formatting.

•   Copyright

    © Pavel Bashkardin, 2022-2024

***************************************************************/

using System.ComponentModel.Design.Serialization;
using System.Globalization;
using System.Reflection;
using System.Security.Permissions;

namespace System.ComponentModel
{

    /// <summary>
    ///    The <see cref="DecimalConverterExtended"/> class extends the functionality 
    ///    of the <see cref="NumberConverterExtended"/> class for converting 
    ///    Decimal (128 bit-precision floating point) values. It supports parsing strings 
    ///    with different number system prefixes (e.g., decimal) and converting 
    ///    Decimal values to their string representations using custom formatting.
    /// </summary>
    [HostProtection(SecurityAction.LinkDemand, SharedState = true)]
    public class DecimalConverterExtended : NumberConverterExtended
    {
        public DecimalConverterExtended() : base(allowBaseEncoding: false, targetType: typeof(decimal)) { }

        /// <summary>
        ///    Converts a string representation of a number to a <see cref="decimal"/> 
        ///    value using the specified radix.
        /// </summary>
        /// <param name="value">
        ///    The string representation of the number.
        /// </param>
        /// <param name="radix">
        ///    The base (radix) to use for conversion is unsupported.
        /// </param>
        /// <returns>
        ///    The converted <see cref="decimal"/> value.
        /// </returns>
        [Obsolete]
        protected override object ConvertFromString(string value, int radix)
        {
            return Convert.ToDecimal(value, CultureInfo.CurrentCulture);
        }

        /// <summary>
        ///    Converts a string representation of a number to a <see cref="decimal"/> 
        ///    value using the specified culture information.
        /// </summary>
        /// <param name="value">
        ///    The string representation of the number.
        /// </param>
        /// <param name="culture">
        ///    The <see cref="CultureInfo"/> used for parsing.
        /// </param>
        /// <returns>
        ///    The converted <see cref="decimal"/> value.
        /// </returns>
        protected override object ConvertFromString(string value, CultureInfo culture)
        {
            return decimal.Parse(value, culture);
        }

        /// <summary>
        ///    Converts a <see cref="decimal"/> value to its string representation 
        ///    using the specified culture information.
        /// </summary>
        /// <param name="value">
        ///    The <see cref="decimal"/> value.
        /// </param>
        /// <param name="culture">
        ///    The <see cref="CultureInfo"/> used for conversion.
        /// </param>
        /// <returns>
        ///    The string representation of the <see cref="decimal"/> value.
        /// </returns>
        protected override string ConvertToString(object value, CultureInfo culture)
        {
            return ((decimal)value).ToString("G", culture);
        }

        /// <inheritdoc />
        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            return destinationType == typeof(InstanceDescriptor) || base.CanConvertTo(context, destinationType);
        }

        /// <inheritdoc />
        public override object ConvertTo(
            ITypeDescriptorContext context,
            CultureInfo culture,
            object value,
            Type destinationType)
        {
            if (destinationType == null)
                throw new ArgumentNullException(nameof(destinationType));
            if (!(destinationType == typeof(InstanceDescriptor)) || !(value is Decimal d))
                return base.ConvertTo(context, culture, value, destinationType);
            object[] arguments = new object[1]
            {
                Decimal.GetBits(d)
            };
            MemberInfo constructor = typeof(Decimal).GetConstructor(new Type[1]
            {
                typeof (int[])
            });
            return constructor != null ? new InstanceDescriptor(constructor, arguments) : (object)null;
        }
    }
}