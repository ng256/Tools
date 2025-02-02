/***************************************************************

•   File: BooleanConverterEx.cs

•   Description

    The  BooleanConverterEx class  extends  the functionality of
    the standard class by providing additional  capabilities for
    converting  bool values. In  particular,  it  allows  you to
    convert  bool values  ​​not  only to the strings  "True"  and
    "False", but  also to  other equivalent representations such
    as "1" or "0",  "On" or "Off",  and "Enabled" or "Disabled".

***************************************************************/


using System.Collections.Generic;
using System.Globalization;
using static System.InternalTools;

namespace System.ComponentModel
{
    /// <summary>
    ///    An extended type converter to convert <see cref="bool" />
    ///    objects to and from various other representations.
    ///    It is an extension of the standard class <see cref="BooleanConverter"/>.
    /// </summary>
    public class BooleanConverterExtended : NumberConverterExtended
    {
        private static volatile TypeConverter.StandardValuesCollection StandartValues
            = new TypeConverter.StandardValuesCollection(new[]
        {
            true,
            false
        });

        private static readonly TypeConverter NumberConverter = new Int32ConverterExtended();

        private static readonly Dictionary<string, bool> Values
            = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase)
        {
            {"1", true},
            {"On", true},
            {"Yes", true},
            {"True", true},
            {"Enable", true},
            {"Enabled", true},

            {"0", false},
            {"No", false},
            {"Off", false},
            {"False", false},
            {"Disable", false},
            {"Disabled", false},
        };

        public BooleanConverterExtended() : base(true, typeof(bool)) { }

        private static bool _registered = false;

        /// <summary>
        ///    Register <see cref="BooleanConverterExtended"/> as default converter for <see cref="bool"/> struct.
        /// </summary>
        public static void Register()
        {
            _registered = _registered || RegisterConverter<bool, BooleanConverterExtended>();
        }

        // Attempts to parse the given object value as a boolean value.
        private static bool TryParse(object value, CultureInfo culture, out bool result)
        {
            result = false;
            object obj;

            switch (value)
            {
                case null:
                    break;

                case bool b:
                    return b;

                case int i:
                    result = i != 0;
                    return true;

                case string s:
                    s = s.TrimWhiteSpaceAndNull();

                    if (s.Length == 0)
                        return false;

                    if (Values.TryGetValue(s, out result))
                        return true;

                    if (NumberConverter.TryConvertFromString(culture, s, out obj))
                    {
                        result = (int)obj != 0;
                        return true;
                    }

                    break;

                case IConvertible conv:
                    if (TryChangeType(typeof(int), culture, value, out obj))
                    {
                        result = (int)obj != 0;
                        return true;
                    }
                    break;

                // For primitive types, try converting the value to an integer and then parsing it.
                default:

                    return false;

            }

            return false;
        }

        /// <summary>
        ///    Converts the given string value to the <see cref="bool" /> object based on the specified number base (radix).
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
        protected override object ConvertFromString(string value, int radix)
        {
            try
            {
                int number = Convert.ToInt32(value, radix);
                return number != 0;
            }
            catch
            {
                throw new ArgumentException(GetResourceString("Format_BadBoolean"));
            }
        }

        /// <summary>
        ///    Converts the given string value to the <see cref="bool" /> object on the specified culture information.
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
        protected override object ConvertFromString(string value, CultureInfo culture)
        {
            try
            {
                int number = Convert.ToInt32(value, culture);
                return number != 0;
            }
            catch
            {
                throw new ArgumentException(GetResourceString("Format_BadBoolean"));
            }
        }

        /// <summary>
        ///    Converts the given <see cref="bool" /> object to a string representation based on the specified culture information.
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
        protected override string ConvertToString(object value, CultureInfo culture)
        {
            return ((bool)value) ? bool.TrueString : bool.FalseString;
        }

        /// <summary>
        ///    Converts the given value object to a Boolean object.
        /// </summary>
        /// <param name="context">
        ///    A <see cref="ITypeDescriptorContext" /> object that provides the format context.
        /// </param>
        /// <param name="culture">
        ///    A <see cref="CultureInfo" /> object that defines the culture to be converted.
        /// </param>
        /// <param name="value">
        ///    The object to be converted <see cref="object" />.
        /// </param>
        /// <returns>
        ///    The <see cref="object" /> object representing the converted value <paramref name="value" />.
        /// </returns>
        /// <exception cref="FormatException">
        ///    The value of <paramref name="value" /> is not a valid value for the final type.
        /// </exception>
        /// <exception cref="NotSupportedException">
        ///    The conversion could not be performed.
        /// </exception>
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            switch (value)
            {
                case null:
                    throw new ArgumentNullException(nameof(value));
                case bool b:
                    return b;
                case string str when (str = str.TrimWhiteSpaceAndNull()).Length > 0 && Values.TryGetValue(str, out bool result):
                    return result;
                default:
                    return base.ConvertFrom(context, culture ?? CultureInfo.CurrentCulture, value);
            }
        }

        /// <summary>
        ///    Returns a value indicating whether this converter can convert an object to the specified type using the specified context.
        /// </summary>
        /// <param name="context">
        ///    A <see cref="ITypeDescriptorContext" /> object that provides the format context.
        /// </param>
        /// <param name="destinationType">
        ///    The <see cref="Type" /> class representing the type to convert to.
        /// </param>
        /// <returns>
        ///    Is <see langword="true" /> if the converter can perform the conversion, otherwise <see langword="false" />.
        /// </returns>
        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            if (destinationType == null)
                throw new ArgumentNullException(nameof(destinationType));

            return destinationType == typeof(bool) || destinationType.IsIntOrString();
        }

        /// <summary>
        ///    Converts the given value to a <see cref="bool" /> object using the specified context and culture information.
        /// </summary>
        /// <param name="context">
        ///    A <see cref="ITypeDescriptorContext" /> object that provides the format context.
        /// </param>
        /// <param name="culture">
        ///     <see cref="CultureInfo" /> object. If <see langword="null" /> is passed, the current culture settings are used.
        /// </param>
        /// <param name="value">
        ///    The object to be converted <see cref="object" />.
        /// </param>
        /// <param name="destinationType">
        ///     <see cref="Type" /> to which the <paramref name="value" /> parameter is converted.
        /// </param>
        /// <returns>
        ///    The <see cref="object" /> object representing the converted value.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///    The parameter <paramref name="destinationType" /> has the value <see langword="null" />.
        /// </exception>
        /// <exception cref="NotSupportedException">
        ///    The conversion could not be performed.
        /// </exception>
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            switch (value)
            {
                case null:
                    throw new ArgumentNullException(nameof(destinationType));
                case bool b:
                    switch (Type.GetTypeCode(destinationType))
                    {
                        case TypeCode.Boolean:
                            return b;
                        case TypeCode.String:
                            return b ? bool.TrueString : bool.FalseString;
                    }

                    break;
            }

            return base.ConvertTo(context, culture ?? CultureInfo.CurrentCulture, value, destinationType);
        }

        /// <inheritdoc />
        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            return StandartValues;
        }
    }
}
