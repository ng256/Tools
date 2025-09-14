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
 *   - XPath-like queries for nested object navigation
 *   - Type-safe argument validation
 *   - JSON parsing with comment support
 *
 * Usage:
 *   - Use `JsValue` as a universal wrapper for JavaScript-like values.
 *   - Create arrays with `JsValue.FromArray` and objects with `JsValue.FromObject`.
 *   - Use in conditions, conversions, and string representations as needed.
 *   - Use XPath-like queries to navigate nested structures: obj.Select("path/to/property")
 *   - Parse JSON strings with comments using JsValue.FromJsonString
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
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

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
        Value = value ?? throw new ArgumentNullException(nameof(value), "String value cannot be null");
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
        if (string.IsNullOrEmpty(input)) return input;
        
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
        values = new List<JsValue>(items ?? throw new ArgumentNullException(nameof(items), "Items collection cannot be null"));
    }

    /// <summary>
    /// Gets or sets the element at the specified index.
    /// </summary>
    public JsValue this[int index]
    {
        get
        {
            if (index < 0 || index >= values.Count)
                throw new IndexOutOfRangeException($"Index {index} is out of range for array with length {values.Count}");
            
            return values[index];
        }
        set
        {
            if (index < 0 || index >= values.Count)
                throw new IndexOutOfRangeException($"Index {index} is out of range for array with length {values.Count}");
            
            values[index] = value ?? throw new ArgumentNullException(nameof(value), "Array element cannot be null");
        }
    }

    /// <summary>
    /// Gets the number of elements in the array.
    /// </summary>
    public int Length => values.Count;

    /// <summary>
    /// Appends a value to the end of the array.
    /// </summary>
    /// <param name="value">The value to append.</param>
    public void Push(JsValue value)
    {
        if (value == null) throw new ArgumentNullException(nameof(value), "Cannot push null value to array");
        values.Add(value);
    }

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
        props = new Dictionary<string, JsValue>(dict ?? throw new ArgumentNullException(nameof(dict), "Dictionary cannot be null"));
    }

    /// <summary>
    /// Gets or sets the property with the specified key.
    /// </summary>
    public JsValue this[string key]
    {
        get
        {
            if (string.IsNullOrEmpty(key)) throw new ArgumentException("Key cannot be null or empty", nameof(key));
            
            return props.ContainsKey(key) ? props[key] : JsUndefined.Instance;
        }
        set
        {
            if (string.IsNullOrEmpty(key)) throw new ArgumentException("Key cannot be null or empty", nameof(key));
            if (value == null) throw new ArgumentNullException(nameof(value), "Property value cannot be null");
            
            props[key] = value;
        }
    }

    /// <summary>
    /// Gets all property names in the object.
    /// </summary>
    public IEnumerable<string> Keys => props.Keys;

    /// <summary>
    /// Gets all property values in the object.
    /// </summary>
    public IEnumerable<JsValue> Values => props.Values;

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
        if (string.IsNullOrEmpty(input)) return input;
        
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

    /// <summary>
    /// Selects a value using XPath-like syntax for navigating nested objects and arrays.
    /// </summary>
    /// <param name="path">XPath-like query path (e.g., "users/[0]/name", "data/items", "config/database/port")</param>
    /// <returns>The selected JsValue or JsUndefined if not found</returns>
    public JsValue Select(string path)
    {
        if (string.IsNullOrEmpty(path))
            throw new ArgumentException("Path cannot be null or empty", nameof(path));

        // Split path by '/' but ignore escaped slashes
        var parts = Regex.Split(path, @"(?<!\\)/")
            .Select(p => p.Replace(@"\/", "/"))
            .Where(p => !string.IsNullOrEmpty(p))
            .ToArray();

        JsValue current = this;

        foreach (var part in parts)
        {
            if (current.IsFalsy() || current.inner == null)
                return JsUndefined.Instance;

            // Array index access: [0], [1], etc.
            if (part.StartsWith("[") && part.EndsWith("]"))
            {
                if (current.inner is JsArray array)
                {
                    var indexStr = part.Substring(1, part.Length - 2);
                    if (int.TryParse(indexStr, out int index) && index >= 0 && index < array.Length)
                    {
                        current = array[index];
                    }
                    else
                    {
                        return JsUndefined.Instance;
                    }
                }
                else
                {
                    return JsUndefined.Instance;
                }
            }
            // Object property access
            else
            {
                if (current.inner is JsObject obj)
                {
                    if (obj.Keys.Contains(part))
                    {
                        current = obj[part];
                    }
                    else
                    {
                        return JsUndefined.Instance;
                    }
                }
                else
                {
                    return JsUndefined.Instance;
                }
            }
        }

        return current;
    }

    /// <summary>
    /// Selects multiple values using XPath-like syntax with wildcards for navigating nested objects and arrays.
    /// </summary>
    /// <param name="path">XPath-like query path with wildcards (e.g., "users/*/name", "data/items/*", "*/name")</param>
    /// <returns>Enumerable of matching JsValues</returns>
    public IEnumerable<JsValue> SelectMany(string path)
    {
        if (string.IsNullOrEmpty(path))
            throw new ArgumentException("Path cannot be null or empty", nameof(path));

        // Split path by '/' but ignore escaped slashes
        var parts = Regex.Split(path, @"(?<!\\)/")
            .Select(p => p.Replace(@"\/", "/"))
            .Where(p => !string.IsNullOrEmpty(p))
            .ToArray();

        var results = new List<JsValue> { this };

        foreach (var part in parts)
        {
            var newResults = new List<JsValue>();

            foreach (var current in results)
            {
                if (current.IsFalsy() || current.inner == null)
                    continue;

                // Wildcard: match all elements or properties
                if (part == "*")
                {
                    if (current.inner is JsArray array)
                    {
                        for (int i = 0; i < array.Length; i++)
                        {
                            newResults.Add(array[i]);
                        }
                    }
                    else if (current.inner is JsObject obj)
                    {
                        foreach (var key in obj.Keys)
                        {
                            newResults.Add(obj[key]);
                        }
                    }
                }
                // Array index access: [0], [1], etc.
                else if (part.StartsWith("[") && part.EndsWith("]"))
                {
                    if (current.inner is JsArray array)
                    {
                        var indexStr = part.Substring(1, part.Length - 2);
                        if (int.TryParse(indexStr, out int index) && index >= 0 && index < array.Length)
                        {
                            newResults.Add(array[index]);
                        }
                    }
                }
                // Object property access
                else
                {
                    if (current.inner is JsObject obj && obj.Keys.Contains(part))
                    {
                        newResults.Add(obj[part]);
                    }
                }
            }

            results = newResults;
            if (results.Count == 0) break;
        }

        foreach (var result in results)
        {
            yield return result;
        }
    }

    private string EscapeJsonString(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;
        
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
    public static implicit operator JsValue(int value) => FromInt32(value);

    /// <summary>
    /// Implicitly converts a double to a JsValue.
    /// </summary>
    public static implicit operator JsValue(double value) => FromDouble(value);

    /// <summary>
    /// Implicitly converts a string to a JsValue.
    /// </summary>
    public static implicit operator JsValue(string value) => FromString(value);

    /// <summary>
    /// Implicitly converts a boolean to a JsValue.
    /// </summary>
    public static implicit operator JsValue(bool value) => FromBoolean(value);

    /// <summary>
    /// Implicitly converts a JsLikeObject to a JsValue.
    /// </summary>
    public static implicit operator JsValue(JsLikeObject value) => new JsValue(value);

    /// <summary>
    /// Implicitly converts an object to a JsValue.
    /// </summary>
    public static implicit operator JsValue(object obj) => FromObject(obj);

    /// <summary>
    /// Creates a JsValue from an array of JsValue.
    /// </summary>
    public static JsValue FromArray(params JsValue[] values)
    {
        if (values == null) throw new ArgumentNullException(nameof(values), "Values array cannot be null");
        return new JsValue(new JsArray(values));
    }

    /// <summary>
    /// Creates a JsValue from a dictionary of properties.
    /// </summary>
    public static JsValue FromObject(Dictionary<string, JsValue> dict)
    {
        if (dict == null) throw new ArgumentNullException(nameof(dict), "Dictionary cannot be null");
        return new JsValue(new JsObject(dict));
    }

    /// <summary>
    /// Creates a JsValue from a Byte value.
    /// </summary>
    public static JsValue FromByte(byte value) => new JsValue(new JsNumber(value));

    /// <summary>
    /// Creates a JsValue from a SByte value.
    /// </summary>
    public static JsValue FromSByte(sbyte value) => new JsValue(new JsNumber(value));

    /// <summary>
    /// Creates a JsValue from an Int16 value.
    /// </summary>
    public static JsValue FromInt16(short value) => new JsValue(new JsNumber(value));

    /// <summary>
    /// Creates a JsValue from a UInt16 value.
    /// </summary>
    public static JsValue FromUInt16(ushort value) => new JsValue(new JsNumber(value));

    /// <summary>
    /// Creates a JsValue from an Int32 value.
    /// </summary>
    public static JsValue FromInt32(int value) => new JsValue(new JsNumber(value));

    /// <summary>
    /// Creates a JsValue from a UInt32 value.
    /// </summary>
    public static JsValue FromUInt32(uint value) => new JsValue(new JsNumber(value));

    /// <summary>
    /// Creates a JsValue from an Int64 value.
    /// </summary>
    public static JsValue FromInt64(long value) => new JsValue(new JsNumber(value));

    /// <summary>
    /// Creates a JsValue from a UInt64 value.
    /// </summary>
    public static JsValue FromUInt64(ulong value) => new JsValue(new JsNumber((double)value));

    /// <summary>
    /// Creates a JsValue from a Single value.
    /// </summary>
    public static JsValue FromSingle(float value) => new JsValue(new JsNumber(value));

    /// <summary>
    /// Creates a JsValue from a Double value.
    /// </summary>
    public static JsValue FromDouble(double value) => new JsValue(new JsNumber(value));

    /// <summary>
    /// Creates a JsValue from a Decimal value.
    /// </summary>
    public static JsValue FromDecimal(decimal value) => new JsValue(new JsNumber((double)value));

    /// <summary>
    /// Creates a JsValue from a Boolean value.
    /// </summary>
    public static JsValue FromBoolean(bool value) => new JsValue(new JsBool(value));

    /// <summary>
    /// Creates a JsValue from a Char value.
    /// </summary>
    public static JsValue FromChar(char value) => new JsValue(new JsString(value.ToString()));

    /// <summary>
    /// Creates a JsValue from a String value.
    /// </summary>
    public static JsValue FromString(string value) => value != null ? new JsValue(new JsString(value)) : new JsValue(null);

    /// <summary>
    /// Creates a JsValue from a DateTime value.
    /// </summary>
    public static JsValue FromDateTime(DateTime value) => new JsValue(new JsString(value.ToString("o")));

    /// <summary>
    /// Creates a JsValue from a DateTimeOffset value.
    /// </summary>
    public static JsValue FromDateTimeOffset(DateTimeOffset value) => new JsValue(new JsString(value.ToString("o")));

    /// <summary>
    /// Creates a JsValue from a TimeSpan value.
    /// </summary>
    public static JsValue FromTimeSpan(TimeSpan value) => new JsValue(new JsString(value.ToString()));

    /// <summary>
    /// Creates a JsValue from a Guid value.
    /// </summary>
    public static JsValue FromGuid(Guid value) => new JsValue(new JsString(value.ToString()));

    // Helper method to check if a struct is a built-in .NET type
    private static bool IsBuiltInStruct(Type type)
    {
        return type == typeof(DateTime) || 
               type == typeof(DateTimeOffset) || 
               type == typeof(TimeSpan) || 
               type == typeof(Guid) || 
               type == typeof(Decimal);
    }

    /// <summary>
    /// Creates a JsValue from any object using reflection to extract public properties.
    /// Supports primitives, classes, dictionaries, collections and common structs.
    /// Rejects custom structs and interfaces.
    /// </summary>
    public static JsValue FromObject(object obj)
    {
        if (obj == null)
        {
            return new JsValue(null);
        }

        Type type = obj.GetType();

        // Handle primitives and special types using the new explicit methods
        if (obj is bool b) return FromBoolean(b);
        else if (obj is string s) return FromString(s);
        else if (obj is byte bt) return FromByte(bt);
        else if (obj is sbyte sbt) return FromSByte(sbt);
        else if (obj is short sh) return FromInt16(sh);
        else if (obj is ushort ush) return FromUInt16(ush);
        else if (obj is int i) return FromInt32(i);
        else if (obj is uint ui) return FromUInt32(ui);
        else if (obj is long l) return FromInt64(l);
        else if (obj is ulong ul) return FromUInt64(ul);
        else if (obj is float f) return FromSingle(f);
        else if (obj is double d) return FromDouble(d);
        else if (obj is decimal dec) return FromDecimal(dec);
        else if (obj is char c) return FromChar(c);
        else if (type.IsEnum) return FromInt32((int)obj);
        // Handle common structs that should be serialized
        else if (obj is DateTime dt) return FromDateTime(dt);
        else if (obj is DateTimeOffset dto) return FromDateTimeOffset(dto);
        else if (obj is TimeSpan ts) return FromTimeSpan(ts);
        else if (obj is Guid guid) return FromGuid(guid);
        // Handle special types
        else if (obj is JsLikeObject jsLike) return new JsValue(jsLike);
        else if (obj is IDictionary dictionary)
        {
            var resultDict = new Dictionary<string, JsValue>();
            foreach (DictionaryEntry entry in dictionary)
            {
                string key = entry.Key?.ToString();
                if (key != null)
                {
                    resultDict[key] = FromObject(entry.Value);
                }
            }
            return FromObject(resultDict);
        }
        else if (obj is IEnumerable enumerable && !(obj is string)) // string already handled above
        {
            var items = new List<JsValue>();
            foreach (var item in enumerable)
            {
                items.Add(FromObject(item));
            }
            return FromArray(items.ToArray());
        }
        else if (type.IsClass || type.IsValueType) // Allow both classes and structs
        {
            // For structs, we'll only serialize them if they're one of the common built-in types
            // Custom structs will be rejected unless they implement specific interfaces
            if (type.IsValueType && !IsBuiltInStruct(type))
            {
                throw new ArgumentException($"Unsupported struct type: {type.Name}. Only built-in structs are supported.");
            }
            
            // Use properties instead of fields for both classes and structs
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                .Where(p => p.CanRead);
            var dict = new Dictionary<string, JsValue>();
            foreach (var prop in properties)
            {
                var value = prop.GetValue(obj);
                dict[prop.Name] = FromObject(value);
            }
            return FromObject(dict);
        }
        else
        {
            throw new ArgumentException($"Unsupported type: {type.Name}.");
        }
    }

    /// <summary>
    /// Parses a JSON string with comment support and returns a JsValue.
    /// Supports single-line (//) and multi-line (/* */) comments.
    /// </summary>
    /// <param name="json">The JSON string to parse, which may include comments</param>
    /// <returns>A JsValue representing the parsed JSON</returns>
    public static JsValue FromJsonString(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return new JsValue(null);

        // Remove comments from the JSON string
        string jsonWithoutComments = RemoveCommentsFromJson(json);
        
        // Parse the cleaned JSON
        return ParseJson(jsonWithoutComments);
    }

    /// <summary>
    /// Removes single-line (//) and multi-line (/* */) comments from a JSON string.
    /// </summary>
    private static string RemoveCommentsFromJson(string json)
    {
        StringBuilder result = new StringBuilder();
        bool inString = false;
        bool inSingleLineComment = false;
        bool inMultiLineComment = false;
        char prevChar = '\0';

        for (int i = 0; i < json.Length; i++)
        {
            char currentChar = json[i];

            if (inSingleLineComment)
            {
                if (currentChar == '\n')
                {
                    inSingleLineComment = false;
                    result.Append(currentChar);
                }
                continue;
            }

            if (inMultiLineComment)
            {
                if (prevChar == '*' && currentChar == '/')
                {
                    inMultiLineComment = false;
                }
                prevChar = currentChar;
                continue;
            }

            if (inString)
            {
                result.Append(currentChar);
                if (currentChar == '"' && prevChar != '\\')
                {
                    inString = false;
                }
            }
            else
            {
                if (currentChar == '"')
                {
                    inString = true;
                    result.Append(currentChar);
                }
                else if (currentChar == '/' && i + 1 < json.Length)
                {
                    char nextChar = json[i + 1];
                    if (nextChar == '/')
                    {
                        inSingleLineComment = true;
                        i++; // Skip the next slash
                    }
                    else if (nextChar == '*')
                    {
                        inMultiLineComment = true;
                        i++; // Skip the asterisk
                    }
                    else
                    {
                        result.Append(currentChar);
                    }
                }
                else
                {
                    result.Append(currentChar);
                }
            }

            prevChar = currentChar;
        }

        return result.ToString();
    }

    /// <summary>
    /// Parses a JSON string without comments and returns a JsValue.
    /// </summary>
    private static JsValue ParseJson(string json)
    {
        int index = 0;
        return ParseValue(json, ref index);
    }

    /// <summary>
    /// Parses a JSON value from the string at the given index.
    /// </summary>
    private static JsValue ParseValue(string json, ref int index)
    {
        SkipWhitespace(json, ref index);
        
        if (index >= json.Length)
            throw new ArgumentException("Unexpected end of JSON string");

        char currentChar = json[index];
        
        if (currentChar == '"') return ParseString(json, ref index);
        if (currentChar == '{') return ParseObject(json, ref index);
        if (currentChar == '[') return ParseArray(json, ref index);
        if (currentChar == 't') return ParseTrue(json, ref index);
        if (currentChar == 'f') return ParseFalse(json, ref index);
        if (currentChar == 'n') return ParseNull(json, ref index);
        if (currentChar == '-' || char.IsDigit(currentChar)) return ParseNumber(json, ref index);
        
        throw new ArgumentException($"Unexpected character '{currentChar}' at position {index}");
    }

    /// <summary>
    /// Skips whitespace characters in the JSON string.
    /// </summary>
    private static void SkipWhitespace(string json, ref int index)
    {
        while (index < json.Length && char.IsWhiteSpace(json[index]))
        {
            index++;
        }
    }

    /// <summary>
    /// Parses a JSON string value.
    /// </summary>
    private static JsValue ParseString(string json, ref int index)
    {
        index++; // Skip opening quote
        StringBuilder sb = new StringBuilder();
        bool escape = false;

        while (index < json.Length)
        {
            char currentChar = json[index];
            
            if (escape)
            {
                switch (currentChar)
                {
                    case '"': sb.Append('"'); break;
                    case '\\': sb.Append('\\'); break;
                    case '/': sb.Append('/'); break;
                    case 'b': sb.Append('\b'); break;
                    case 'f': sb.Append('\f'); break;
                    case 'n': sb.Append('\n'); break;
                    case 'r': sb.Append('\r'); break;
                    case 't': sb.Append('\t'); break;
                    case 'u': 
                        // Unicode escape sequence
                        if (index + 4 < json.Length)
                        {
                            string hex = json.Substring(index + 1, 4);
                            sb.Append((char)Convert.ToInt32(hex, 16));
                            index += 4;
                        }
                        break;
                    default: sb.Append(currentChar); break;
                }
                escape = false;
            }
            else
            {
                if (currentChar == '\\')
                {
                    escape = true;
                }
                else if (currentChar == '"')
                {
                    index++; // Skip closing quote
                    return new JsValue(new JsString(sb.ToString()));
                }
                else
                {
                    sb.Append(currentChar);
                }
            }
            
            index++;
        }

        throw new ArgumentException("Unterminated string in JSON");
    }

    /// <summary>
    /// Parses a JSON object.
    /// </summary>
    private static JsValue ParseObject(string json, ref int index)
    {
        index++; // Skip opening brace
        SkipWhitespace(json, ref index);
        
        var dict = new Dictionary<string, JsValue>();
        
        while (index < json.Length)
        {
            if (json[index] == '}')
            {
                index++; // Skip closing brace
                return FromObject(dict);
            }
            
            // Parse key
            if (json[index] != '"')
                throw new ArgumentException($"Expected '\"' at position {index}, found '{json[index]}'");
            
            JsValue keyValue = ParseString(json, ref index);
            string key = keyValue.ToJsString();
            
            SkipWhitespace(json, ref index);
            
            // Parse colon
            if (json[index] != ':')
                throw new ArgumentException($"Expected ':' at position {index}, found '{json[index]}'");
            
            index++;
            SkipWhitespace(json, ref index);
            
            // Parse value
            JsValue value = ParseValue(json, ref index);
            dict[key] = value;
            
            SkipWhitespace(json, ref index);
            
            // Parse comma or closing brace
            if (json[index] == ',')
            {
                index++;
                SkipWhitespace(json, ref index);
            }
            else if (json[index] == '}')
            {
                // Continue to closing brace handling
            }
            else
            {
                throw new ArgumentException($"Expected ',' or '}}' at position {index}, found '{json[index]}'");
            }
        }
        
        throw new ArgumentException("Unterminated object in JSON");
    }

    /// <summary>
    /// Parses a JSON array.
    /// </summary>
    private static JsValue ParseArray(string json, ref int index)
    {
        index++; // Skip opening bracket
        SkipWhitespace(json, ref index);
        
        var values = new List<JsValue>();
        
        while (index < json.Length)
        {
            if (json[index] == ']')
            {
                index++; // Skip closing bracket
                return FromArray(values.ToArray());
            }
            
            // Parse value
            JsValue value = ParseValue(json, ref index);
            values.Add(value);
            
            SkipWhitespace(json, ref index);
            
            // Parse comma or closing bracket
            if (json[index] == ',')
            {
                index++;
                SkipWhitespace(json, ref index);
            }
            else if (json[index] == ']')
            {
                // Continue to closing bracket handling
            }
            else
            {
                throw new ArgumentException($"Expected ',' or ']' at position {index}, found '{json[index]}'");
            }
        }
        
        throw new ArgumentException("Unterminated array in JSON");
    }

    /// <summary>
    /// Parses the JSON 'true' value.
    /// </summary>
    private static JsValue ParseTrue(string json, ref int index)
    {
        if (index + 3 < json.Length && json.Substring(index, 4) == "true")
        {
            index += 4;
            return new JsValue(new JsBool(true));
        }
        throw new ArgumentException($"Unexpected token at position {index}");
    }

    /// <summary>
    /// Parses the JSON 'false' value.
    /// </summary>
    private static JsValue ParseFalse(string json, ref int index)
    {
        if (index + 4 < json.Length && json.Substring(index, 5) == "false")
        {
            index += 5;
            return new JsValue(new JsBool(false));
        }
        throw new ArgumentException($"Unexpected token at position {index}");
    }

    /// <summary>
    /// Parses the JSON 'null' value.
    /// </summary>
    private static JsValue ParseNull(string json, ref int index)
    {
        if (index + 3 < json.Length && json.Substring(index, 4) == "null")
        {
            index += 4;
            return new JsValue(null);
        }
        throw new ArgumentException($"Unexpected token at position {index}");
    }

    /// <summary>
    /// Parses a JSON number value.
    /// </summary>
    private static JsValue ParseNumber(string json, ref int index)
    {
        int start = index;
        
        // Handle negative numbers
        if (json[index] == '-')
        {
            index++;
        }
        
        // Integer part
        while (index < json.Length && char.IsDigit(json[index]))
        {
            index++;
        }
        
        // Decimal part
        if (index < json.Length && json[index] == '.')
        {
            index++;
            while (index < json.Length && char.IsDigit(json[index]))
            {
                index++;
            }
        }
        
        // Exponent part
        if (index < json.Length && (json[index] == 'e' || json[index] == 'E'))
        {
            index++;
            if (index < json.Length && (json[index] == '+' || json[index] == '-'))
            {
                index++;
            }
            while (index < json.Length && char.IsDigit(json[index]))
            {
                index++;
            }
        }
        
        string numberStr = json.Substring(start, index - start);
        if (double.TryParse(numberStr, NumberStyles.Float, CultureInfo.InvariantCulture, out double result))
        {
            return new JsValue(new JsNumber(result));
        }
        
        throw new ArgumentException($"Invalid number format at position {start}");
    }

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
        if (conversionType == null) throw new ArgumentNullException(nameof(conversionType));
        
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
        try
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
            
            // Test XPath-like queries
            var complexData = JsValue.FromObject(new Dictionary<string, JsValue>
            {
                ["users"] = JsValue.FromArray(
                    JsValue.FromObject(new Dictionary<string, JsValue>
                    {
                        ["id"] = 1,
                        ["name"] = "Alice",
                        ["email"] = "alice@example.com"
                    }),
                    JsValue.FromObject(new Dictionary<string, JsValue>
                    {
                        ["id"] = 2,
                        ["name"] = "Bob",
                        ["email"] = "bob@example.com"
                    })
                ),
                ["config"] = JsValue.FromObject(new Dictionary<string, JsValue>
                {
                    ["database"] = JsValue.FromObject(new Dictionary<string, JsValue>
                    {
                        ["host"] = "localhost",
                        ["port"] = 5432,
                        ["credentials"] = JsValue.FromObject(new Dictionary<string, JsValue>
                        {
                            ["username"] = "admin",
                            ["password"] = "secret"
                        })
                    })
                })
            });
            
            // Test single value selection
            Console.WriteLine("\nXPath Tests:");
            Console.WriteLine("First user name: " + complexData.Select("users/[0]/name").ToJsString()); // "Alice"
            Console.WriteLine("Database host: " + complexData.Select("config/database/host").ToJsString()); // "localhost"
            Console.WriteLine("Non-existent: " + complexData.Select("nonexistent/property").ToJsString()); // "undefined"
            
            // Test multi-value selection with wildcards
            Console.WriteLine("\nAll user names:");
            foreach (var userName in complexData.SelectMany("users/*/name"))
            {
                Console.WriteLine(" - " + userName.ToJsString());
            }
            
            Console.WriteLine("\nAll database properties:");
            foreach (var dbProperty in complexData.SelectMany("config/database/*"))
            {
                Console.WriteLine(" - " + dbProperty.ToJsString());
            }
            
            // Test new FromObject method with anonymous objects
            Console.WriteLine("\nTesting FromObject with anonymous objects:");
            var anonymousObj = new { Name = "Test", Value = 123, Active = true };
            JsValue jsFromAnonymous = anonymousObj;
            Console.WriteLine("Anonymous object JSON: " + jsFromAnonymous.ToJsonString(true));
            
            // Test with nested anonymous objects
            var nestedAnonymous = new {
                User = new { Name = "Alice", Age = 25 },
                Items = new[] { "one", "two", "three" }
            };
            JsValue jsFromNested = nestedAnonymous;
            Console.WriteLine("Nested anonymous object JSON: " + jsFromNested.ToJsonString(true));
            
            // Test new explicit factory methods
            Console.WriteLine("\nTesting explicit factory methods:");
            JsValue byteVal = JsValue.FromByte(255);
            JsValue intVal = JsValue.FromInt32(42);
            JsValue doubleVal = JsValue.FromDouble(3.14);
            JsValue dateVal = JsValue.FromDateTime(DateTime.Now);
            JsValue guidVal = JsValue.FromGuid(Guid.NewGuid());
            
            Console.WriteLine("Byte: " + byteVal.ToJsonString());
            Console.WriteLine("Int: " + intVal.ToJsonString());
            Console.WriteLine("Double: " + doubleVal.ToJsonString());
            Console.WriteLine("DateTime: " + dateVal.ToJsonString());
            Console.WriteLine("Guid: " + guidVal.ToJsonString());
            
            // Test JSON parsing with comments
            Console.WriteLine("\nTesting JSON parsing with comments:");
            string jsonWithComments = @"
            {
                // This is a single-line comment
                ""name"": ""John"",
                ""age"": 30,
                /* This is a
                   multi-line comment */
                ""isStudent"": false,
                ""scores"": [90, 85, 95] // trailing comment
            }";
            
            JsValue parsedFromJson = JsValue.FromJsonString(jsonWithComments);
            Console.WriteLine("Parsed JSON with comments: " + parsedFromJson.ToJsonString(true));
            
            // Test error cases
            try
            {
                // This should throw an exception
                var invalid = new JsString(null);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nExpected error: {ex.Message}");
            }
            
            // Test rejection of structs
            try
            {
                var point = new System.Drawing.Point(10, 20);
                JsValue jsFromStruct = point;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nExpected rejection of struct: {ex.Message}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unexpected error: {ex.Message}");
        }
    }
}
