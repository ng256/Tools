/*
 * JsLike: A C# Library for JavaScript-like Value Semantics
 *
 * This library provides a set of classes and utilities to emulate JavaScript's
 * truthy/falsy evaluation, type coercion, and object/array handling in C#.
 * It is designed for scenarios where you need to bridge C# and JavaScript logic,
 * such as in scripting engines, configuration parsers, or dynamic data processing.
 *
 * Features:
 *   - Truthy/Falsy evaluation (e.g., `if (value)` works as in JavaScript)
 *   - JavaScript-like types: Undefined, Number, String, Boolean, Array, Object
 *   - JSON serialization and string representation
 *   - Implicit and explicit conversions to/from native C# types
 *   - Full IConvertible implementation for seamless integration with .NET APIs
 *
 * Usage:
 *   - Use `JsValue` as a universal wrapper for JavaScript-like values.
 *   - Create arrays with `JsValue.FromArray` and objects with `JsValue.FromObject`.
 *   - Use in conditions, conversions, and string representations as needed.
 *
 * License: MIT (see below)
 */

// MIT License
//
// Copyright (c) 2025 Pavel Bashkardin
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;

/// <summary>
/// Base class for JavaScript-like truthy/falsy evaluation in C#.
/// Provides the foundation for all JS-like types and their boolean evaluation.
/// </summary>
public abstract class JsLikeObject
{
    /// <summary>
    /// Determines if the object is falsy according to JavaScript semantics.
    /// </summary>
    /// <returns>True if the object is falsy, otherwise false.</returns>
    public abstract bool IsFalsy();

    /// <summary>
    /// Allows the object to be used in a boolean context (true).
    /// </summary>
    public static bool operator true(JsLikeObject obj)
    {
        return obj != null && !obj.IsFalsy();
    }

    /// <summary>
    /// Allows the object to be used in a boolean context (false).
    /// </summary>
    public static bool operator false(JsLikeObject obj)
    {
        return obj == null || obj.IsFalsy();
    }

    /// <summary>
    /// Implicit conversion to bool for use in conditions.
    /// </summary>
    public static implicit operator bool(JsLikeObject obj)
    {
        return obj != null && !obj.IsFalsy();
    }
}

/// <summary>
/// Represents the JavaScript undefined type.
/// </summary>
[DebuggerDisplay("undefined")]
public class JsUndefined : JsLikeObject
{
    /// <summary>
    /// Singleton instance of JsUndefined.
    /// </summary>
    public static readonly JsUndefined Instance = new JsUndefined();

    private JsUndefined() { }

    /// <summary>
    /// Always returns true, as undefined is falsy in JavaScript.
    /// </summary>
    public override bool IsFalsy() => true;

    /// <summary>
    /// Returns the string representation of undefined.
    /// </summary>
    public override string ToString() => "undefined";

    /// <summary>
    /// Returns the JSON string representation of undefined.
    /// </summary>
    /// <param name="prettyPrint">Whether to format the output with indentation (ignored for undefined).</param>
    /// <param name="indentLevel">The current indentation level (ignored for undefined).</param>
    /// <returns>"undefined"</returns>
    public string ToJsonString(bool prettyPrint = false, int indentLevel = 0) => "undefined";
}

/// <summary>
/// Represents a JavaScript-like number.
/// </summary>
[DebuggerDisplay("{ToJsonString(),nq}")]
public class JsNumber : JsLikeObject
{
    /// <summary>
    /// The numeric value.
    /// </summary>
    public double Value { get; private set; }

    /// <summary>
    /// Initializes a new instance of JsNumber.
    /// </summary>
    /// <param name="value">The numeric value.</param>
    public JsNumber(double value)
    {
        Value = value;
    }

    /// <summary>
    /// Determines if the number is falsy (0 or NaN).
    /// </summary>
    public override bool IsFalsy()
    {
        return Value == 0.0 || double.IsNaN(Value);
    }

    /// <summary>
    /// Returns the string representation of the number.
    /// </summary>
    public override string ToString()
    {
        return Value.ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Returns the JSON string representation of the number.
    /// </summary>
    /// <param name="prettyPrint">Whether to format the output with indentation (ignored for numbers).</param>
    /// <param name="indentLevel">The current indentation level (ignored for numbers).</param>
    /// <returns>String representation of the number or "NaN"</returns>
    public string ToJsonString(bool prettyPrint = false, int indentLevel = 0)
    {
        return double.IsNaN(Value) ? "NaN" : Value.ToString(CultureInfo.InvariantCulture);
    }
}

/// <summary>
/// Represents a JavaScript-like string.
/// </summary>
[DebuggerDisplay("{ToJsonString(),nq}")]
public class JsString : JsLikeObject
{
    /// <summary>
    /// The string value.
    /// </summary>
    public string Value { get; private set; }

    /// <summary>
    /// Initializes a new instance of JsString.
    /// </summary>
    /// <param name="value">The string value.</param>
    public JsString(string value)
    {
        Value = value;
    }

    /// <summary>
    /// Determines if the string is falsy (null or empty).
    /// </summary>
    public override bool IsFalsy()
    {
        return string.IsNullOrEmpty(Value);
    }

    /// <summary>
    /// Returns the string representation.
    /// </summary>
    public override string ToString()
    {
        return Value;
    }

    /// <summary>
    /// Returns the JSON string representation.
    /// </summary>
    /// <param name="prettyPrint">Whether to format the output with indentation (ignored for strings).</param>
    /// <param name="indentLevel">The current indentation level (ignored for strings).</param>
    /// <returns>JSON-escaped string representation</returns>
    public string ToJsonString(bool prettyPrint = false, int indentLevel = 0)
    {
        return $"\"{EscapeJsonString(Value)}\"";
    }

    private string EscapeJsonString(string input)
    {
        return input.Replace("\\", "\\\\")
                   .Replace("\"", "\\\"")
                   .Replace("\b", "\\b")
                   .Replace("\f", "\\f")
                   .Replace("\n", "\\n")
                   .Replace("\r", "\\r")
                   .Replace("\t", "\\t");
    }
}

/// <summary>
/// Represents a JavaScript-like boolean.
/// </summary>
[DebuggerDisplay("{ToJsonString(),nq}")]
public class JsBool : JsLikeObject
{
    /// <summary>
    /// The boolean value.
    /// </summary>
    public bool Value { get; private set; }

    /// <summary>
    /// Initializes a new instance of JsBool.
    /// </summary>
    /// <param name="value">The boolean value.</param>
    public JsBool(bool value)
    {
        Value = value;
    }

    /// <summary>
    /// Determines if the boolean is falsy (false).
    /// </summary>
    public override bool IsFalsy()
    {
        return !Value;
    }

    /// <summary>
    /// Returns the string representation of the boolean.
    /// </summary>
    public override string ToString()
    {
        return Value ? "true" : "false";
    }

    /// <summary>
    /// Returns the JSON string representation of the boolean.
    /// </summary>
    /// <param name="prettyPrint">Whether to format the output with indentation (ignored for booleans).</param>
    /// <param name="indentLevel">The current indentation level (ignored for booleans).</param>
    /// <returns>"true" or "false"</returns>
    public string ToJsonString(bool prettyPrint = false, int indentLevel = 0)
    {
        return Value ? "true" : "false";
    }
}

/// <summary>
/// Represents a JavaScript-like array.
/// </summary>
[DebuggerDisplay("{ToJsonString(),nq}")]
public class JsArray : JsLikeObject
{
    private List<JsValue> values;

    /// <summary>
    /// Initializes a new instance of JsArray.
    /// </summary>
    /// <param name="items">The initial items in the array.</param>
    public JsArray(IEnumerable<JsValue> items)
    {
        values = new List<JsValue>(items);
    }

    /// <summary>
    /// Gets or sets the element at the specified index.
    /// </summary>
    public JsValue this[int index]
    {
        get => values[index];
        set => values[index] = value;
    }

    /// <summary>
    /// Gets the number of elements in the array.
    /// </summary>
    public int Length => values.Count;

    /// <summary>
    /// Appends a value to the end of the array.
    /// </summary>
    /// <param name="value">The value to append.</param>
    public void Push(JsValue value) => values.Add(value);

    /// <summary>
    /// Arrays are always truthy.
    /// </summary>
    public override bool IsFalsy() => false;

    /// <summary>
    /// Returns a JavaScript-like string representation of the array.
    /// </summary>
    public string ToJsString()
    {
        List<string> parts = new List<string>();
        foreach (var v in values) parts.Add(v.ToJsString());
        return string.Join(",", parts);
    }

    /// <summary>
    /// Returns the JSON string representation of the array.
    /// </summary>
    /// <param name="prettyPrint">Whether to format the output with indentation.</param>
    /// <param name="indentLevel">The current indentation level.</param>
    /// <returns>JSON array representation</returns>
    public string ToJsonString(bool prettyPrint = false, int indentLevel = 0)
    {
        if (values.Count == 0) return "[]";

        var indent = prettyPrint ? new string(' ', indentLevel * 2) : "";
        var childIndent = prettyPrint ? new string(' ', (indentLevel + 1) * 2) : "";
        var newLine = prettyPrint ? "\n" : "";

        StringBuilder sb = new StringBuilder("[");
        sb.Append(newLine);

        for (int i = 0; i < values.Count; i++)
        {
            if (i > 0)
            {
                sb.Append(",");
                sb.Append(newLine);
            }
            
            if (prettyPrint) sb.Append(childIndent);
            sb.Append(values[i].ToJsonString(prettyPrint, indentLevel + 1));
        }

        sb.Append(newLine);
        if (prettyPrint) sb.Append(indent);
        sb.Append("]");

        return sb.ToString();
    }

    /// <summary>
    /// Returns the string representation of the array.
    /// </summary>
    public override string ToString() => ToJsString();
}

/// <summary>
/// Represents a JavaScript-like object (dictionary).
/// </summary>
[DebuggerDisplay("{ToJsonString(),nq}")]
public class JsObject : JsLikeObject
{
    private Dictionary<string, JsValue> props;

    /// <summary>
    /// Initializes a new instance of JsObject.
    /// </summary>
    /// <param name="dict">The initial properties of the object.</param>
    public JsObject(Dictionary<string, JsValue> dict)
    {
        props = new Dictionary<string, JsValue>(dict);
    }

    /// <summary>
    /// Gets or sets the property with the specified key.
    /// </summary>
    public JsValue this[string key]
    {
        get => props.ContainsKey(key) ? props[key] : JsUndefined.Instance;
        set => props[key] = value;
    }

    /// <summary>
    /// Objects are always truthy.
    /// </summary>
    public override bool IsFalsy() => false;

    /// <summary>
    /// Returns a JavaScript-like string representation of the object.
    /// </summary>
    public string ToJsString() => "[object Object]";

    /// <summary>
    /// Returns the JSON string representation of the object.
    /// </summary>
    /// <param name="prettyPrint">Whether to format the output with indentation.</param>
    /// <param name="indentLevel">The current indentation level.</param>
    /// <returns>JSON object representation</returns>
    public string ToJsonString(bool prettyPrint = false, int indentLevel = 0)
    {
        if (props.Count == 0) return "{}";

        var indent = prettyPrint ? new string(' ', indentLevel * 2) : "";
        var childIndent = prettyPrint ? new string(' ', (indentLevel + 1) * 2) : "";
        var newLine = prettyPrint ? "\n" : "";

        StringBuilder sb = new StringBuilder("{");
        sb.Append(newLine);

        bool first = true;
        foreach (var kvp in props)
        {
            if (!first)
            {
                sb.Append(",");
                sb.Append(newLine);
            }
            
            if (prettyPrint) sb.Append(childIndent);
            sb.Append($"\"{EscapeJsonString(kvp.Key)}\": ");
            sb.Append(kvp.Value.ToJsonString(prettyPrint, indentLevel + 1));
            first = false;
        }

        sb.Append(newLine);
        if (prettyPrint) sb.Append(indent);
        sb.Append("}");

        return sb.ToString();
    }

    private string EscapeJsonString(string input)
    {
        return input.Replace("\\", "\\\\")
                   .Replace("\"", "\\\"")
                   .Replace("\b", "\\b")
                   .Replace("\f", "\\f")
                   .Replace("\n", "\\n")
                   .Replace("\r", "\\r")
                   .Replace("\t", "\\t");
    }

    /// <summary>
    /// Returns the string representation of the object.
    /// </summary>
    public override string ToString() => ToJsString();
}

/// <summary>
/// Universal wrapper for JavaScript-like values, with full JavaScript semantics and IConvertible implementation.
/// </summary>
[DebuggerDisplay("{ToJsonString(),nq}")]
public class JsValue : JsLikeObject, IConvertible
{
    private JsLikeObject inner;

    private JsValue(JsLikeObject inner)
    {
        this.inner = inner;
    }

    /// <summary>
    /// Determines if the value is falsy according to JavaScript semantics.
    /// </summary>
    public override bool IsFalsy()
    {
        return inner == null || inner.IsFalsy();
    }

    /// <summary>
    /// Returns the string representation of the value.
    /// </summary>
    public override string ToString()
    {
        return inner == null ? "null" : inner.ToString();
    }

    /// <summary>
    /// Converts the value to a number according to JavaScript semantics.
    /// </summary>
    public double ToNumber()
    {
        if (inner == null) return 0.0;
        if (inner is JsUndefined) return double.NaN;
        if (inner is JsNumber n) return n.Value;
        if (inner is JsString s)
        {
            return double.TryParse(s.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out double result)
                ? result : double.NaN;
        }
        if (inner is JsBool b) return b.Value ? 1.0 : 0.0;
        return double.NaN;
    }

    /// <summary>
    /// Converts the value to a boolean according to JavaScript semantics.
    /// </summary>
    public bool ToBool()
    {
        if (inner == null || inner is JsUndefined) return false;
        if (inner is JsNumber n) return n.Value != 0.0 && !double.IsNaN(n.Value);
        if (inner is JsString s) return !string.IsNullOrEmpty(s.Value);
        if (inner is JsBool b) return b.Value;
        return true;
    }

    /// <summary>
    /// Returns a JavaScript-like string representation of the value.
    /// </summary>
    public string ToJsString()
    {
        if (inner == null) return "null";
        if (inner is JsUndefined) return "undefined";
        if (inner is JsNumber n) return double.IsNaN(n.Value) ? "NaN" : n.Value.ToString(CultureInfo.InvariantCulture);
        if (inner is JsBool b) return b.Value ? "true" : "false";
        if (inner is JsString s) return s.Value;
        if (inner is JsArray a) return a.ToJsString();
        if (inner is JsObject o) return o.ToJsString();
        return inner.ToString();
    }

    /// <summary>
    /// Returns the JSON string representation of the value.
    /// </summary>
    /// <param name="prettyPrint">Whether to format the output with indentation.</param>
    /// <param name="indentLevel">The current indentation level.</param>
    /// <returns>JSON representation of the value</returns>
    public string ToJsonString(bool prettyPrint = false, int indentLevel = 0)
    {
        if (inner == null) return "null";
        if (inner is JsUndefined) return "undefined";
        if (inner is JsNumber n) return n.ToJsonString(prettyPrint, indentLevel);
        if (inner is JsBool b) return b.ToJsonString(prettyPrint, indentLevel);
        if (inner is JsString s) return s.ToJsonString(prettyPrint, indentLevel);
        if (inner is JsArray a) return a.ToJsonString(prettyPrint, indentLevel);
        if (inner is JsObject o) return o.ToJsonString(prettyPrint, indentLevel);
        return inner.ToString();
    }

    private string EscapeJsonString(string input)
    {
        return input.Replace("\\", "\\\\")
                   .Replace("\"", "\\\"")
                   .Replace("\b", "\\b")
                   .Replace("\f", "\\f")
                   .Replace("\n", "\\n")
                   .Replace("\r", "\\r")
                   .Replace("\t", "\\t");
    }

    // Implicit conversions (factory behavior)
    /// <summary>
    /// Implicitly converts an integer to a JsValue.
    /// </summary>
    public static implicit operator JsValue(int value) => new JsValue(new JsNumber(value));

    /// <summary>
    /// Implicitly converts a double to a JsValue.
    /// </summary>
    public static implicit operator JsValue(double value) => new JsValue(new JsNumber(value));

    /// <summary>
    /// Implicitly converts a string to a JsValue.
    /// </summary>
    public static implicit operator JsValue(string value) => new JsValue(value != null ? new JsString(value) : null);

    /// <summary>
    /// Implicitly converts a boolean to a JsValue.
    /// </summary>
    public static implicit operator JsValue(bool value) => new JsValue(new JsBool(value));

    /// <summary>
    /// Implicitly converts a JsLikeObject to a JsValue.
    /// </summary>
    public static implicit operator JsValue(JsLikeObject value) => new JsValue(value);

    /// <summary>
    /// Creates a JsValue from an array of JsValue.
    /// </summary>
    public static JsValue FromArray(params JsValue[] values) => new JsValue(new JsArray(values));

    /// <summary>
    /// Creates a JsValue from a dictionary of properties.
    /// </summary>
    public static JsValue FromObject(Dictionary<string, JsValue> dict) => new JsValue(new JsObject(dict));

    // Explicit conversions to native types
    /// <summary>
    /// Explicitly converts a JsValue to a double.
    /// </summary>
    public static explicit operator double(JsValue v) => v.ToNumber();

    /// <summary>
    /// Explicitly converts a JsValue to an integer.
    /// </summary>
    public static explicit operator int(JsValue v) => (int)v.ToNumber();

    /// <summary>
    /// Explicitly converts a JsValue to a boolean.
    /// </summary>
    public static explicit operator bool(JsValue v) => v.ToBool();

    /// <summary>
    /// Explicitly converts a JsValue to a string.
    /// </summary>
    public static explicit operator string(JsValue v) => v.ToJsString();

    /// <summary>
    /// Gets the inner JsLikeObject.
    /// </summary>
    public JsLikeObject Inner => inner;

    #region IConvertible Implementation
    /// <summary>
    /// Returns the TypeCode for this value.
    /// </summary>
    public TypeCode GetTypeCode()
    {
        if (inner == null) return TypeCode.Empty;
        if (inner is JsUndefined) return TypeCode.Object;
        if (inner is JsNumber) return TypeCode.Double;
        if (inner is JsString) return TypeCode.String;
        if (inner is JsBool) return TypeCode.Boolean;
        return TypeCode.Object;
    }

    /// <summary>
    /// Converts the value to a boolean.
    /// </summary>
    public bool ToBoolean(IFormatProvider provider) => ToBool();

    /// <summary>
    /// Converts the value to a byte.
    /// </summary>
    public byte ToByte(IFormatProvider provider) => (byte)ToNumber();

    /// <summary>
    /// Converts the value to a char.
    /// </summary>
    public char ToChar(IFormatProvider provider) => ToJsString()[0];

    /// <summary>
    /// Converts the value to a DateTime.
    /// </summary>
    public DateTime ToDateTime(IFormatProvider provider) => DateTime.Parse(ToJsString(), provider);

    /// <summary>
    /// Converts the value to a decimal.
    /// </summary>
    public decimal ToDecimal(IFormatProvider provider) => (decimal)ToNumber();

    /// <summary>
    /// Converts the value to a double.
    /// </summary>
    public double ToDouble(IFormatProvider provider) => ToNumber();

    /// <summary>
    /// Converts the value to a short.
    /// </summary>
    public short ToInt16(IFormatProvider provider) => (short)ToNumber();

    /// <summary>
    /// Converts the value to an int.
    /// </summary>
    public int ToInt32(IFormatProvider provider) => (int)ToNumber();

    /// <summary>
    /// Converts the value to a long.
    /// </summary>
    public long ToInt64(IFormatProvider provider) => (long)ToNumber();

    /// <summary>
    /// Converts the value to a sbyte.
    /// </summary>
    public sbyte ToSByte(IFormatProvider provider) => (sbyte)ToNumber();

    /// <summary>
    /// Converts the value to a float.
    /// </summary>
    public float ToSingle(IFormatProvider provider) => (float)ToNumber();

    /// <summary>
    /// Converts the value to a string.
    /// </summary>
    public string ToString(IFormatProvider provider) => ToJsString();

    /// <summary>
    /// Converts the value to a ushort.
    /// </summary>
    public ushort ToUInt16(IFormatProvider provider) => (ushort)ToNumber();

    /// <summary>
    /// Converts the value to a uint.
    /// </summary>
    public uint ToUInt32(IFormatProvider provider) => (uint)ToNumber();

    /// <summary>
    /// Converts the value to a ulong.
    /// </summary>
    public ulong ToUInt64(IFormatProvider provider) => (ulong)ToNumber();

    /// <summary>
    /// Converts the value to the specified type.
    /// </summary>
    public object ToType(Type conversionType, IFormatProvider provider)
    {
        if (conversionType == typeof(bool)) return ToBoolean(provider);
        if (conversionType == typeof(byte)) return ToByte(provider);
        if (conversionType == typeof(char)) return ToChar(provider);
        if (conversionType == typeof(DateTime)) return ToDateTime(provider);
        if (conversionType == typeof(decimal)) return ToDecimal(provider);
        if (conversionType == typeof(double)) return ToDouble(provider);
        if (conversionType == typeof(short)) return ToInt16(provider);
        if (conversionType == typeof(int)) return ToInt32(provider);
        if (conversionType == typeof(long)) return ToInt64(provider);
        if (conversionType == typeof(sbyte)) return ToSByte(provider);
        if (conversionType == typeof(float)) return ToSingle(provider);
        if (conversionType == typeof(string)) return ToString(provider);
        if (conversionType == typeof(ushort)) return ToUInt16(provider);
        if (conversionType == typeof(uint)) return ToUInt32(provider);
        if (conversionType == typeof(ulong)) return ToUInt64(provider);
        if (conversionType == typeof(JsValue)) return this;
        if (conversionType == typeof(object)) return this;
        throw new InvalidCastException($"Cannot convert JsValue to type {conversionType.Name}");
    }
    #endregion
}

class Program
{
    static void Main()
    {
        // Test basic values
        JsValue num = 42;
        JsValue str = "hello";
        JsValue boolean = true;
        JsValue nil = (JsValue)null;
        JsValue undef = JsUndefined.Instance;
        // Test IConvertible implementation
        Console.WriteLine(Convert.ToDouble(num));    // 42
        Console.WriteLine(Convert.ToString(str));    // "hello"
        Console.WriteLine(Convert.ToBoolean(boolean)); // true
        Console.WriteLine(Convert.ToString(undef));  // "undefined"
        // Test arrays and objects with JSON serialization
        var arr = JsValue.FromArray(1, "two", true);
        Console.WriteLine("Array JS: " + arr.ToJsString());    // "1,two,true"
        Console.WriteLine("Array JSON: " + arr.ToJsonString()); // "[1,\"two\",true]"
        Console.WriteLine("Array Pretty JSON: " + arr.ToJsonString(true)); // Formatted JSON
        
        var objDict = new Dictionary<string, JsValue>
        {
            ["name"] = "John",
            ["age"] = 30,
            ["isStudent"] = false
        };
        var obj = JsValue.FromObject(objDict);
        Console.WriteLine("Object JS: " + obj.ToJsString());    // "[object Object]"
        Console.WriteLine("Object JSON: " + obj.ToJsonString()); // "{"name":"John","age":30,"isStudent":false}"
        Console.WriteLine("Object Pretty JSON: " + obj.ToJsonString(true)); // Formatted JSON
        
        // Test truthy/falsy in conditions
        if (num) Console.WriteLine("Number is truthy");
        if (str) Console.WriteLine("String is truthy");
        if (!nil) Console.WriteLine("Null is falsy");
        if (!undef) Console.WriteLine("Undefined is falsy");
    }
}
