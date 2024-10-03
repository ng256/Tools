using System.Globalization;

namespace System.ComponentModel
{
	internal class ModuleMessageFormatterConverter : TypeConverter
	{
		private static TypeDescriptionProvider _attributeProvider;

		static ModuleMessageFormatterConverter()
		{
			_attributeProvider = null;
		}

		public static void Register()
		{
			if (_attributeProvider == null)
			{
				_attributeProvider = TypeDescriptor.AddAttributes(typeof(ModuleMessageFormatter), new TypeConverterAttribute(typeof(ModuleMessageFormatterConverter)));
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
			string fileNmae;
			if ((fileNmae = value as string) != null)
			{
				return new ModuleMessageFormatter(fileNmae);
			}
			return base.ConvertFrom(context, culture, value);
		}

		public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
		{
			ModuleMessageFormatter formatter;
			if ((formatter = value as ModuleMessageFormatter) != null && destinationType == typeof(string))
			{
				return formatter.FileName;
			}
			return base.ConvertTo(context, culture, value, destinationType);
		}
	}
}
