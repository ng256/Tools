using System.Collections;
using System.ComponentModel.Design.Serialization;
using System.Globalization;
using System.Security.Permissions;

namespace System.ComponentModel
{
    /// <summary>
    ///     Provides a type converter to convert <see cref="Enum" /> objects to and from various other representations.
    /// </summary>
    [HostProtection(SecurityAction.LinkDemand, SharedState = true)]
    public class EnumConverterExtended : NumberConverterExtended
    {
        private StandardValuesCollection _values;
        private Type _type;

        /// <summary>
        ///     Initializes a new instance of the <see cref="EnumConverterExtended" /> class for the given type.
        /// </summary>
        /// <param name="type">
        ///     A <see cref="Type" /> that represents the type of enumeration to associate with this enumeration converter.
        /// </param>
        public EnumConverterExtended(Type type) : base(typeof(Enum))
        {
            _type = type;
        }

        /// <summary>
        ///     Specifies the type of the enumerator this converter is associated with.
        /// </summary>
        /// <returns>
        ///     The type of the enumerator this converter is associated with.
        /// </returns>
        protected Type EnumType => _type;

        /// <summary>
        ///     Gets or sets a <see cref="TypeConverter.StandardValuesCollection" /> that specifies the possible values for the enumeration.
        /// </summary>
        /// <returns>
        ///     A <see cref="TypeConverter.StandardValuesCollection" /> that specifies the possible values for the enumeration.
        /// </returns>
        protected StandardValuesCollection Values
        {
            get => _values;
            set => _values = value;
        }

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
            return Enum.ToObject(_type, Convert.ToInt32(value, radix));
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
            if (value.IndexOf(',') < 0)
                return Enum.Parse(_type, value, true);
            int result = 0;
            foreach (string flag in value.Split(','))
            {
                result |= Convert.ToInt32((Enum)Enum.Parse(_type, flag, true), culture);
            }
            return Enum.ToObject(_type, result);
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
            return value is IConvertible conv
                ? conv.ToString(culture)
                : value.ToString();
        }

        /// <summary>
        ///     Gets a value indicating whether this converter can convert an object
        ///     in the given source type to an enumeration object using the specified context.
        /// </summary>
        /// <returns>
        ///     true if this converter can perform the conversion; otherwise, false.
        /// </returns>
        /// <param name="context">
        ///     An <see cref="ITypeDescriptorContext" /> that provides a format context.
        /// </param>
        /// <param name="sourceType">
        ///     A <see cref="Type" /> that represents the type you wish to convert from.
        /// </param>
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return sourceType == typeof(string)
                   || sourceType == typeof(Enum[])
                   || base.CanConvertFrom(context, sourceType);
        }

        /// <summary>
        ///     Gets a value indicating whether this converter can convert an object to the given destination type using the context.
        /// </summary>
        /// <returns>
        ///     true if this converter can perform the conversion; otherwise, false.
        /// </returns>
        /// <param name="context">
        ///     An <see cref="ITypeDescriptorContext" /> that provides a format context.
        /// </param>
        /// <param name="destinationType">
        ///     A <see cref="Type" /> that represents the type you wish to convert to.
        /// </param>
        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            return destinationType == typeof(InstanceDescriptor)
                   || destinationType == typeof(Enum[])
                   || base.CanConvertTo(context, destinationType);
        }

        /// <summary>
        ///     Gets an <see cref="Collections.IComparer" /> that can be used to sort the values of the enumeration.
        /// </summary>
        /// <returns>
        ///     An <see cref="Collections.IComparer" /> for sorting the enumeration values.
        /// </returns>
        protected virtual IComparer Comparer => StringComparer.InvariantCultureIgnoreCase;

        /// <summary>
        ///     Converts the specified value object to an enumeration object.
        /// </summary>
        /// <returns>
        ///     An <see cref="Object" /> that represents the converted <paramref name="value" />.
        /// </returns>
        /// <param name="context">
        ///     An <see cref="ITypeDescriptorContext" /> that provides a format context. 
        /// </param>
        /// <param name="culture">
        ///     An optional <see cref="Globalization.CultureInfo" />. If not supplied, the current culture is assumed.
        /// </param>
        /// <param name="value">
        ///     The <see cref="Object" /> to convert.
        /// </param>
        /// <exception cref="FormatException">
        ///     <paramref name="value" /> is not a valid value for the target type.
        /// </exception>
        /// <exception cref="NotSupportedException">
        ///     The conversion cannot be performed.
        /// </exception>
        public override object ConvertFrom(
          ITypeDescriptorContext context,
          CultureInfo culture,
          object value)
        {
            switch (value)
            {
                case string str:
                    return base.ConvertFrom(context, culture, str);
                case Enum[] array:
                    long result = 0;
                    foreach (Enum item in array)
                        result |= Convert.ToInt64(item, culture);
                    return Enum.ToObject(_type, result);
                default:
                    return base.ConvertFrom(context, culture, value);
            }
        }

        /// <summary>
        ///     Converts the given value object to the specified destination type.
        /// </summary>
        /// <returns>
        ///     An <see cref="Object" /> that represents the converted <paramref name="value" />.
        /// </returns>
        /// <param name="context">
        ///     An <see cref="ITypeDescriptorContext" /> that provides a format context. 
        /// </param>
        /// <param name="culture">
        ///     An optional <see cref="Globalization.CultureInfo" />. If not supplied, the current culture is assumed.
        /// </param>
        /// <param name="value">
        ///     The <see cref="Object" /> to convert. 
        /// </param>
        /// <param name="destinationType">
        ///     The <see cref="Type" /> to convert the value to. 
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="destinationType" /> is null. 
        /// </exception>
        /// <exception cref="ArgumentException">
        ///     <paramref name="value" /> is not a valid value for the enumeration. 
        /// </exception>
        /// <exception cref="NotSupportedException">
        ///     The conversion cannot be performed. 
        /// </exception>
        public override object ConvertTo(
          ITypeDescriptorContext context,
          CultureInfo culture,
          object value,
          Type destinationType)
        {
            if (destinationType == null)
                throw new ArgumentNullException(nameof(destinationType));
            if (destinationType == typeof(string) && value != null)
            {
                Type underlyingType = Enum.GetUnderlyingType(_type);
                if (value is IConvertible && value.GetType() != underlyingType)
                    value = ((IConvertible)value).ToType(underlyingType, culture);
                return value.ToString();
            }
            if (destinationType == typeof(Enum[]))
            {
                return new Enum[] { (Enum)value };
            }
            return base.ConvertTo(context, culture, value, destinationType);
        }
    }
}
