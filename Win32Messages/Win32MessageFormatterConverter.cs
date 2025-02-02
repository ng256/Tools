using System.Globalization;

namespace System.ComponentModel
{
	internal class Win32MessageFormatterConverter : TypeConverter
	{
		private static TypeDescriptionProvider _attributeProvider;

		static Win32MessageFormatterConverter()
		{
			_attributeProvider = null;
		}

		public static void Register()
		{
			if (_attributeProvider == null)
			{
				_attributeProvider = TypeDescriptor.AddAttributes(typeof(Win32MessageFormatter), new TypeConverterAttribute(typeof(Win32MessageFormatterConverter)));
			}
		}

		public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
		{
			if (!(sourceType == typeof(string)) && !(sourceType == typeof(int)))
			{
				return base.CanConvertFrom(context, sourceType);
			}
			return true;
		}

		public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
		{
			if (!(destinationType == typeof(string)) && !(destinationType == typeof(int)))
			{
				return base.CanConvertTo(context, destinationType);
			}
			return true;
		}

		public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
		{
			object obj;
			if ((obj = value) is int)
			{
				int lcid = (int)obj;
				return new Win32MessageFormatter(lcid);
			}
			CultureInfo c;
			if ((c = value as CultureInfo) != null)
			{
				return new Win32MessageFormatter(c);
			}
			string name;
			if ((name = value as string) != null)
			{
				if (int.TryParse(name, NumberStyles.Integer, culture, out var lcid))
				{
					return new Win32MessageFormatter(lcid);
				}
				return new Win32MessageFormatter(name);
			}
			return base.ConvertFrom(context, culture, value);
		}

		public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
		{
			Win32MessageFormatter formatter;
			if ((formatter = value as Win32MessageFormatter) != null)
			{
				if (destinationType == typeof(int))
				{
					return formatter.LCID;
				}
				if (destinationType == typeof(string))
				{
					return new CultureInfo(formatter.LCID).Name;
				}
				if (destinationType == typeof(CultureInfo))
				{
					return new CultureInfo(formatter.LCID);
				}
			}
			return base.ConvertTo(context, culture, value, destinationType);
		}
	}
}
