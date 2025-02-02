/***************************************************************

•   File: NumberConverterExtended.cs

•   Description

    The NumberConverterExtended is an abstract  class provides a
    base  implementation for converting  numeric  values  to and
    from  their  string   representations. This   class includes
    methods that  handle strings with prefixes indicating number
    bases (binary, octal,  hexadecimal),  with optional  support
    for base encoding control.


•   Copyright

    © Pavel Bashkardin, 2022-2024

***************************************************************/

using System.Globalization;
using System.Security.Permissions;

namespace System.ComponentModel
{
    /// <summary>
    ///    Abstract base class providing functionality for converting numeric values to and from their string representations.
    /// </summary>
    [HostProtection(SecurityAction.LinkDemand, SharedState = true)]
    public abstract class NumberConverterExtended : TypeConverter
    {
        /// <summary>
        ///    Indicates if encoding with specific base prefixes (e.g., hexadecimal, binary) is allowed.
        /// </summary>
        protected bool AllowBaseEncoding = true;

        /// <summary>
        ///    Specifies the target type to which the conversion will be performed.
        /// </summary>
        protected Type TargetType;

        /// <summary>
        ///    Converts the given string value to the target type based on the specified number base (radix).
        /// </summary>
        /// <param name="value">
        ///    The string to convert.
        /// </param>
        /// <param name="radix">
        ///    The numeric base (e.g., 2 for binary, 16 for hexadecimal).
        /// </param>
        /// <returns>
        ///    An object representing the converted value.
        /// </returns>
        protected abstract object ConvertFromString(string value, int radix);

        /// <summary>
        ///    Converts the given string value to the target type based on the specified culture information.
        /// </summary>
        /// <param name="value">
        ///    The string to convert.
        /// </param>
        /// <param name="culture">
        ///    The culture information to use during conversion.
        /// </param>
        /// <returns>
        ///    An object representing the converted value.
        /// </returns>
        protected abstract object ConvertFromString(string value, CultureInfo culture);

        /// <summary>
        ///    Converts the given value to a string representation based on the specified culture information.
        /// </summary>
        /// <param name="value">
        ///    The value to convert.
        /// </param>
        /// <param name="culture">
        ///    The culture information to use during conversion.
        /// </param>
        /// <returns>
        ///    A string representation of the value.
        /// </returns>
        protected abstract string ConvertToString(object value, CultureInfo culture);

        /// <summary>
        ///    Initializes a new instance of the BaseNumberConverterExtended class with the specified target type.
        /// </summary>
        /// <param name="targetType">
        ///    The target type for conversions.
        /// </param>
        protected NumberConverterExtended(Type targetType)
        {
            TargetType = targetType;
        }

        /// <summary>
        ///    Initializes a new instance of the BaseNumberConverterExtended class with the specified encoding option and target type.
        /// </summary>
        /// <param name="allowBaseEncoding">
        ///    Specifies whether base encoding is allowed.
        /// </param>
        /// <param name="targetType">
        ///    The target type for conversions.
        /// </param>
        protected NumberConverterExtended(bool allowBaseEncoding, Type targetType)
        {
            AllowBaseEncoding = allowBaseEncoding;
            TargetType = targetType;
        }

        /// <inheritdoc />
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return sourceType.IsPrimitive || sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
        }

        /// <inheritdoc />
        public override object ConvertFrom(ITypeDescriptorContext context, 
            CultureInfo culture, object value)
        {
            // Use case-insensitive comparison for base prefixes
            const StringComparison comparison = StringComparison.OrdinalIgnoreCase;

            if (value == null)
                throw new ArgumentNullException(nameof(value));

            // If culture is not provided, use the current culture
            if (culture == null)
                culture = CultureInfo.CurrentCulture;

            // Process the input based on its type
            switch (value)
            {
                // Handle string input
                case string str when (str = str.Trim()).Length > 0:

                    // Handle encoding-specific prefixes if allowed.
                    if (AllowBaseEncoding)
                    {
                        // Hexadecimal format, prefix #, &, $, 0x, &h or suffix h.
                        if (str[0] == '#' || str[0] == '&' || str[0] == '$')
                            return ConvertFromString(str.Substring(1), 16);

                        if (str.EndsWith("h", comparison))
                            return ConvertFromString(str.Substring(0, str.Length - 1), 16);

                        if (str.StartsWith("0x", comparison)
                            || str.StartsWith("&h", comparison))
                            return ConvertFromString(str.Substring(2), 16);

                        // Binary format, prefix %, 0b or suffix b.
                        if (str[0] == '%')
                            return ConvertFromString(str.Substring(1), 2);

                        if (str.StartsWith("0b", comparison))
                            return ConvertFromString(str.Substring(2), 2);

                        if (str.EndsWith("b", comparison))
                            return ConvertFromString(str.Substring(0, str.Length - 1), 2);

                        // Octal format, prefixes 0o, &o, 8# or suffix o.
                        if (str.StartsWith("0o", comparison)
                            || str.StartsWith("&o", comparison)
                            || str.StartsWith("8#"))
                            return ConvertFromString(str.Substring(2), 8);

                        if (str.EndsWith("o", comparison))
                            return ConvertFromString(str.Substring(0, str.Length - 1), 8);
                    }

                    // Default to parsing the string as a decimal number.
                    return ConvertFromString(str, culture);

                // Handle convertible types by changing them to the target type
                case IConvertible conv:
                    return Convert.ChangeType(conv, TargetType, culture);
            }

            // Fall back to base conversion if no cases match
            return base.ConvertFrom(context, culture, value);
        }

        /// <inheritdoc />
        public override object ConvertTo(ITypeDescriptorContext context, 
            CultureInfo culture, object value, Type destinationType)
        {
            if (value == null)
                throw new ArgumentNullException();

            if (destinationType == null)
                throw new ArgumentNullException(nameof(destinationType));

            if (destinationType.IsInstanceOfType(value))
                return value;

            if (culture == null)
                culture = CultureInfo.CurrentCulture;

            if (destinationType == typeof(string) && TargetType.IsInstanceOfType(value))
                return ConvertToString(value, culture);

            if (destinationType.IsPrimitive)
                return Convert.ChangeType(value, destinationType, culture);
            
            return base.ConvertTo(context, culture, value, destinationType);
        }

        /// <inheritdoc />
        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            return destinationType.IsPrimitive || base.CanConvertTo(context, destinationType);
        }
    }
}
