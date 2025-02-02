/***************************************************************

•   File: CultureInfoConverter.cs

•   Description

    CultureInfoConverter is  a class that provides a unified way
    to convert CultureInfo objects to various data types such as
    int, string, and others. This    can be useful  when working
    with       different        cultures     and      languages.

    The CultureInfoConverter class   also has a  static Register
    method that registers the CultureInfoConverter as a standard
    converter    for the    CultureInfo    class.   This  allows
    CultureInfoConverter    to  be  used    to  convert  default
    CultureInfo objects.

***************************************************************/

using System.Globalization;
using System.Security.Permissions;
using static System.InternalTools;

namespace System.ComponentModel
{
    /// <summary>
    ///    Provides a unified way to convert objects of type <see cref="CultureInfo"/> to other types.
    /// </summary>
    [HostProtection(SecurityAction.LinkDemand, SharedState = true)]
    public class CultureInfoConverterExtended : CultureInfoConverter
    {
        private static readonly TypeConverter NumberConverter = new Int32ConverterExtended();

        private static bool _registered;

        /// <summary>
        ///    Register <see cref="CultureInfoConverterExtended"/> as default converter for <see cref="CultureInfo"/> class.
        /// </summary>
        public static void Register()
        {
            _registered = _registered || RegisterConverter<CultureInfo, CultureInfoConverterExtended>();
        }

        static CultureInfoConverterExtended()
        {
            _registered = false;
            Register();
        }

        // Selects a culture based on various inputs.
        private CultureInfo GetCultureInfo(CultureInfo culture, object value)
        {
            switch (value)
            {
                case CultureInfo c:
                    return c;
                case int lcid:
                    return CultureInfo.GetCultureInfo(lcid);
                case string name when (name = name.Trim()).Length > 0:
                {
                    return NumberConverter.TryConvertFromString(culture, name, out object lcid) 
                        ? CultureInfo.GetCultureInfo((int)lcid) 
                        : CultureInfo.GetCultureInfo(name);
                }
                default:
                {
                    if (value.GetType().IsPrimitive) return TryChangeType(typeof(int), culture, value, out object lcid)
                        ? CultureInfo.GetCultureInfo((int)lcid)
                        : null;
                    return null;
                }
            }
        }

        /// <summary>
        ///    Returns a value indicating whether this converter can convert an object of the given type
        ///    to the <see cref="CultureInfo"/> type using the given context.
        /// </summary>
        /// <param name="context">
        ///    A <see cref="ITypeDescriptorContext" /> object that provides the format context.
        /// </param>
        /// <param name="sourceType">
        ///    A <see cref="Type" /> object representing the type from which to convert.
        /// </param>
        /// <returns>
        ///    Is <see langword="true" /> if the converter can perform the conversion, otherwise <see langword="false" />.
        /// </returns>
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            if (sourceType == null)
                throw new ArgumentNullException(nameof(sourceType));

            TypeCode typeCode = Type.GetTypeCode(sourceType);
            return typeCode > TypeCode.Char && typeCode < TypeCode.DateTime; // any number value.
        }

        /// <summary>
        ///    Converts the specified object to the <see cref="CultureInfo"/> type, using the specified context and culture information.
        /// </summary>
        /// <param name="context">
        ///    A <see cref="ITypeDescriptorContext" /> object that provides the format context.
        /// </param>
        /// <param name="culture">
        ///    The <see cref="CultureInfo" /> object used as the current culture.
        ///    If <see langword="null" /> is passed, the invariant culture settings are used.
        /// </param>
        /// <param name="value">
        ///    The object to be converted <see cref="object" />.
        /// </param>
        /// <returns>
        ///     <see cref="object" /> representing the converted value.
        /// </returns>
        /// <exception cref="NotSupportedException">
        ///    The conversion could not be performed.
        /// </exception>
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            return GetCultureInfo(culture ?? CultureInfo.InvariantCulture, value) ?? base.ConvertFrom(context, culture, value);
        }

        /// <summary>
        ///    Returns a value indicating whether this converter can object to <see cref="CultureInfo"/> using the specified context.
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

            return destinationType == typeof(CultureInfo) || destinationType.IsIntOrString();
        }

        /// <param name="context">
        ///    A <see cref="ITypeDescriptorContext" /> object that provides the format context.
        /// </param>
        /// <param name="culture">
        /// <see cref="CultureInfo" /> object. If <see langword="null" /> is passed, the invariant culture settings are used.
        /// </param>
        /// <param name="value">
        ///    The object to be converted <see cref="object" />.
        /// </param>
        /// <param name="destinationType">
        ///     <see cref="Type" /> to which the <paramref name="value" /> parameter is converted.</param>
        /// <returns>
        ///     <see cref="object" /> representing the converted value.</returns>
        /// <exception cref="ArgumentNullException">The parameter <paramref name="destinationType" /> has the value <see langword="null" />.
        /// </exception>
        /// <exception cref="NotSupportedException">
        ///    The conversion could not be performed.
        /// </exception>
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == null)
                throw new ArgumentNullException(nameof(destinationType));

            switch (Type.GetTypeCode(destinationType))
            {
                case TypeCode.Object
                    when destinationType == typeof(CultureInfo)
                         && GetCultureInfo(culture ?? CultureInfo.InvariantCulture, value) is CultureInfo c:
                    return c;
                case TypeCode.Int32 when value is CultureInfo c:
                    return c.LCID;
                case TypeCode.String when value is CultureInfo c:
                    return c.Name;
                default:
                    return base.ConvertTo(context, culture, value, destinationType);
            }
        }
    }
}
