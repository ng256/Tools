using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;

public class EnumConverter<T> : TypeConverter where T : struct, Enum
{
    // Check if conversion from the given source type is supported
    public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
    {
        return sourceType == typeof(string) || sourceType == typeof(int) || base.CanConvertFrom(context, sourceType);
    }

    // Check if conversion to the given destination type is supported
    public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
    {
        return destinationType == typeof(string) || destinationType == typeof(int) || base.CanConvertTo(context, destinationType);
    }

    // Convert from string or integer to Enum type
    public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
    {
        if (value is string stringValue)
        {
            if (TryParse(stringValue, ignoreCase: true, out T result))
            {
                return result;
            }
        }
        else if (value is int intValue && Enum.IsDefined(typeof(T), intValue))
        {
            return (T)Enum.ToObject(typeof(T), intValue);
        }
        
        throw new ArgumentException($"Cannot convert '{value}' to {typeof(T).Name}");
    }

    // Convert Enum type to string or integer
    public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
    {
        if (value is T enumValue)
        {
            if (destinationType == typeof(string))
            {
                return enumValue.ToString();
            }
            if (destinationType == typeof(int))
            {
                return Convert.ToInt32(enumValue);
            }
        }
        
        throw new ArgumentException($"Cannot convert '{value}' to {destinationType.Name}");
    }

    // TryParse method to parse either string or integer values to Enum
    public static bool TryParse(string value, bool ignoreCase, out T result)
    {
        result = default;

        // Try to parse as integer first
        if (int.TryParse(value, out int numericValue))
        {
            if (Enum.IsDefined(typeof(T), numericValue))
            {
                result = (T)Enum.ToObject(typeof(T), numericValue);
                return true;
            }
            return false;
        }

        // Try to parse by name
        return Enum.TryParse(value, ignoreCase, out result) && Enum.IsDefined(typeof(T), result);
    }

    // Get all values of the Enum
    public static IEnumerable<T> GetValues()
    {
        return (T[])Enum.GetValues(typeof(T));
    }
}
