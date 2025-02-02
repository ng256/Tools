using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design.Serialization;
using System.Globalization;
using System.Reflection;
using System.Security.Permissions;

namespace System.ComponentModel
{
    /// <summary>Provides a type converter to convert <see cref="T:System.Enum" /> objects to and from various other representations.</summary>
    [HostProtection(SecurityAction.LinkDemand, SharedState = true)]
    public class EnumConverter : TypeConverter
    {
        [Serializable]
        internal class InvariantComparer : IComparer
        {
            private CompareInfo m_compareInfo;
            internal static readonly InvariantComparer Default = new InvariantComparer();

            internal InvariantComparer() => m_compareInfo = CultureInfo.InvariantCulture.CompareInfo;

            public int Compare(object a, object b)
            {
                string string1 = a as string;
                string string2 = b as string;
                return string1 != null && string2 != null ? m_compareInfo.Compare(string1, string2) : Collections.Comparer.Default.Compare(a, b);
            }
        }

        private StandardValuesCollection values;
        private Type type;

        /// <summary>Initializes a new instance of the <see cref="T:System.ComponentModel.EnumConverter" /> class for the given type.</summary>
        /// <param name="type">A <see cref="T:System.Type" /> that represents the type of enumeration to associate with this enumeration converter. </param>
        public EnumConverter(Type type) => this.type = type;

        /// <summary>Specifies the type of the enumerator this converter is associated with.</summary>
        /// <returns>The type of the enumerator this converter is associated with.</returns>
        protected Type EnumType => type;

        /// <summary>Gets or sets a <see cref="T:System.ComponentModel.TypeConverter.StandardValuesCollection" /> that specifies the possible values for the enumeration.</summary>
        /// <returns>A <see cref="T:System.ComponentModel.TypeConverter.StandardValuesCollection" /> that specifies the possible values for the enumeration.</returns>
        protected StandardValuesCollection Values
        {
            get => values;
            set => values = value;
        }

        /// <summary>Gets a value indicating whether this converter can convert an object in the given source type to an enumeration object using the specified context.</summary>
        /// <returns>true if this converter can perform the conversion; otherwise, false.</returns>
        /// <param name="context">An <see cref="T:System.ComponentModel.ITypeDescriptorContext" /> that provides a format context. </param>
        /// <param name="sourceType">A <see cref="T:System.Type" /> that represents the type you wish to convert from. </param>
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return sourceType == typeof(string) 
                   || sourceType == typeof(Enum[]) 
                   || base.CanConvertFrom(context, sourceType);
        }

        /// <summary>Gets a value indicating whether this converter can convert an object to the given destination type using the context.</summary>
        /// <returns>true if this converter can perform the conversion; otherwise, false.</returns>
        /// <param name="context">An <see cref="T:System.ComponentModel.ITypeDescriptorContext" /> that provides a format context. </param>
        /// <param name="destinationType">A <see cref="T:System.Type" /> that represents the type you wish to convert to. </param>
        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            return destinationType == typeof(InstanceDescriptor) 
                   || destinationType == typeof(Enum[]) 
                   || base.CanConvertTo(context, destinationType);
        }

        /// <summary>Gets an <see cref="T:System.Collections.IComparer" /> that can be used to sort the values of the enumeration.</summary>
        /// <returns>An <see cref="T:System.Collections.IComparer" /> for sorting the enumeration values.</returns>
        protected virtual IComparer Comparer => InvariantComparer.Default;

        /// <summary>Converts the specified value object to an enumeration object.</summary>
        /// <returns>An <see cref="T:System.Object" /> that represents the converted <paramref name="value" />.</returns>
        /// <param name="context">An <see cref="T:System.ComponentModel.ITypeDescriptorContext" /> that provides a format context. </param>
        /// <param name="culture">An optional <see cref="T:System.Globalization.CultureInfo" />. If not supplied, the current culture is assumed. </param>
        /// <param name="value">The <see cref="T:System.Object" /> to convert. </param>
        /// <exception cref="T:System.FormatException">
        /// <paramref name="value" /> is not a valid value for the target type. </exception>
        /// <exception cref="T:System.NotSupportedException">The conversion cannot be performed. </exception>
        public override object ConvertFrom(
          ITypeDescriptorContext context,
          CultureInfo culture,
          object value)
        {
            switch (value)
            {
                case string _:
                    string str1 = (string)value;
                    if (str1.IndexOf(',') == -1)
                        return Enum.Parse(type, str1, true);
                    long num = 0;
                    string str2 = str1;
                    char[] chArray = new char[1] { ',' };
                    foreach (string str3 in str2.Split(chArray))
                        num |= Convert.ToInt64((Enum)Enum.Parse(type, str3, true), culture);
                    return Enum.ToObject(type, num);
                case Enum[] _:
                    long num1 = 0;
                    foreach (Enum @enum in (Enum[])value)
                        num1 |= Convert.ToInt64(@enum, culture);
                    return Enum.ToObject(type, num1);
                default:
                    return base.ConvertFrom(context, culture, value);
            }
        }

        /// <summary>Converts the given value object to the specified destination type.</summary>
        /// <returns>An <see cref="T:System.Object" /> that represents the converted <paramref name="value" />.</returns>
        /// <param name="context">An <see cref="T:System.ComponentModel.ITypeDescriptorContext" /> that provides a format context. </param>
        /// <param name="culture">An optional <see cref="T:System.Globalization.CultureInfo" />. If not supplied, the current culture is assumed. </param>
        /// <param name="value">The <see cref="T:System.Object" /> to convert. </param>
        /// <param name="destinationType">The <see cref="T:System.Type" /> to convert the value to. </param>
        /// <exception cref="T:System.ArgumentNullException">
        /// <paramref name="destinationType" /> is null. </exception>
        /// <exception cref="T:System.ArgumentException">
        /// <paramref name="value" /> is not a valid value for the enumeration. </exception>
        /// <exception cref="T:System.NotSupportedException">The conversion cannot be performed. </exception>
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
                Type underlyingType = Enum.GetUnderlyingType(type);
                if (value is IConvertible && value.GetType() != underlyingType)
                    value = ((IConvertible)value).ToType(underlyingType, culture);
                return type.IsDefined(typeof(FlagsAttribute), false)
                       || Enum.IsDefined(type, value)
                    ? Enum.Format(type, value, "G")
                    : base.ConvertTo(context, culture, value, destinationType);
            }
            if (destinationType == typeof(InstanceDescriptor) && value != null)
            {
                string invariantString = ConvertToInvariantString(context, value);
                if (this.type.IsDefined(typeof(FlagsAttribute), false) && invariantString.IndexOf(',') != -1)
                {
                    Type underlyingType = Enum.GetUnderlyingType(this.type);
                    if (value is IConvertible)
                    {
                        object type = ((IConvertible)value).ToType(underlyingType, culture);
                        MethodInfo method = typeof(Enum).GetMethod("ToObject", new Type[2]
                        {
              typeof (Type),
              underlyingType
                        });
                        if (method != null)
                            return new InstanceDescriptor(method, new object[2]
                            {
                                this.type,
                                type
                            });
                    }
                }
                else
                {
                    FieldInfo field = type.GetField(invariantString);
                    if (field != null)
                        return new InstanceDescriptor(field, null);
                }
            }
            if (!(destinationType == typeof(Enum[])) || value == null)
                return base.ConvertTo(context, culture, value, destinationType);
            if (this.type.IsDefined(typeof(FlagsAttribute), false))
            {
                List<Enum> enumList = new List<Enum>();
                Array values = Enum.GetValues(type);
                long[] numArray = new long[values.Length];
                for (int index = 0; index < values.Length; ++index)
                    numArray[index] = Convert.ToInt64((Enum)values.GetValue(index), culture);
                long int64 = Convert.ToInt64((Enum)value, culture);
                bool flag = true;
                while (flag)
                {
                    flag = false;
                    foreach (long num in numArray)
                    {
                        if (num != 0L && (num & int64) == num || num == int64)
                        {
                            enumList.Add((Enum)Enum.ToObject(type, num));
                            flag = true;
                            int64 &= ~num;
                            break;
                        }
                    }
                    if (int64 == 0L)
                        break;
                }
                if (!flag && int64 != 0L)
                    enumList.Add((Enum)Enum.ToObject(type, int64));
                return enumList.ToArray();
            }
            return new Enum[1]
            {
                (Enum) Enum.ToObject(this.type, value)
            };
        }

        /// <summary>Gets a collection of standard values for the data type this validator is designed for.</summary>
        /// <returns>A <see cref="T:System.ComponentModel.TypeConverter.StandardValuesCollection" /> that holds a standard set of valid values, or null if the data type does not support a standard set of values.</returns>
        /// <param name="context">An <see cref="T:System.ComponentModel.ITypeDescriptorContext" /> that provides a format context. </param>
        public override StandardValuesCollection GetStandardValues(
          ITypeDescriptorContext context)
        {
            if (values == null)
            {
                Type type = TypeDescriptor.GetReflectionType(this.type);
                if (type == null)
                    type = this.type;
                FieldInfo[] fields = type.GetFields(BindingFlags.Static | BindingFlags.Public);
                ArrayList arrayList = null;
                if (fields != null && fields.Length != 0)
                    arrayList = new ArrayList(fields.Length);
                if (arrayList != null)
                {
                    foreach (FieldInfo fieldInfo in fields)
                    {
                        BrowsableAttribute browsableAttribute = null;
                        foreach (Attribute customAttribute in fieldInfo.GetCustomAttributes(typeof(BrowsableAttribute), false))
                            browsableAttribute = customAttribute as BrowsableAttribute;
                        if (browsableAttribute == null || browsableAttribute.Browsable)
                        {
                            object obj = null;
                            try
                            {
                                if (fieldInfo.Name != null)
                                    obj = Enum.Parse(this.type, fieldInfo.Name);
                            }
                            catch (ArgumentException ex)
                            {
                            }
                            if (obj != null)
                                arrayList.Add(obj);
                        }
                    }
                    IComparer comparer = Comparer;
                    if (comparer != null)
                        arrayList.Sort(comparer);
                }
                values = new StandardValuesCollection(arrayList?.ToArray());
            }
            return values;
        }

        /// <summary>Gets a value indicating whether the list of standard values returned from <see cref="M:System.ComponentModel.TypeConverter.GetStandardValues" /> is an exclusive list using the specified context.</summary>
        /// <returns>true if the <see cref="T:System.ComponentModel.TypeConverter.StandardValuesCollection" /> returned from <see cref="M:System.ComponentModel.TypeConverter.GetStandardValues" /> is an exhaustive list of possible values; false if other values are possible.</returns>
        /// <param name="context">An <see cref="T:System.ComponentModel.ITypeDescriptorContext" /> that provides a format context. </param>
        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context) => !type.IsDefined(typeof(FlagsAttribute), false);

        /// <summary>Gets a value indicating whether this object supports a standard set of values that can be picked from a list using the specified context.</summary>
        /// <returns>true because <see cref="M:System.ComponentModel.TypeConverter.GetStandardValues" /> should be called to find a common set of values the object supports. This method never returns false.</returns>
        /// <param name="context">An <see cref="T:System.ComponentModel.ITypeDescriptorContext" /> that provides a format context. </param>
        public override bool GetStandardValuesSupported(ITypeDescriptorContext context) => true;

        /// <summary>Gets a value indicating whether the given object value is valid for this type.</summary>
        /// <returns>true if the specified value is valid for this object; otherwise, false.</returns>
        /// <param name="context">An <see cref="T:System.ComponentModel.ITypeDescriptorContext" /> that provides a format context. </param>
        /// <param name="value">The <see cref="T:System.Object" /> to test. </param>
        public override bool IsValid(ITypeDescriptorContext context, object value) => Enum.IsDefined(type, value);
    }
}