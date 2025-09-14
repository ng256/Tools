# JsLike: JavaScript-like Value Semantics in C#

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

JsLike is a C# library that emulates JavaScript's truthy/falsy evaluation, type coercion, and object/array handling. It is designed for scenarios where you need to bridge C# and JavaScript logic, such as in scripting engines, configuration parsers, or dynamic data processing.

## Features

- **Truthy/Falsy Evaluation**: Use values directly in boolean contexts, just like in JavaScript.
- **JavaScript-like Types**: `Undefined`, `Number`, `String`, `Boolean`, `Array`, and `Object`.
- **JSON Serialization**: Convert values to JSON strings for easy serialization and debugging.
- **Type Conversion**: Implicit and explicit conversions to/from native C# types.
- **IConvertible Support**: Full integration with .NET's `IConvertible` for seamless use with standard APIs.

## Installation

1. Clone this repository or download the source code.
2. Add the project to your solution or reference the compiled DLL in your C# project.

## Usage

### Basic Values

```csharp
JsValue num = 42;
JsValue str = "hello";
JsValue boolean = true;
JsValue nil = (JsValue)null;
JsValue undef = JsUndefined.Instance;
```

### Truthy/Falsy in Conditions
```csharp
if (num) Console.WriteLine("Number is truthy");
if (str) Console.WriteLine("String is truthy");
if (!nil) Console.WriteLine("Null is falsy");
if (!undef) Console.WriteLine("Undefined is falsy");
```

### Arrays and Objects
```csharp
var arr = JsValue.FromArray(1, "two", true);
Console.WriteLine(arr.ToJsonString()); // "[1,"two",true]"

var objDict = new Dictionary<string, JsValue>
{
    ["name"] = "John",
    ["age"] = 30,
    ["isStudent"] = false
};
var obj = JsValue.FromObject(objDict);
Console.WriteLine(obj.ToJsonString()); // "{"name":"John","age":30,"isStudent"\:false}"
```

### Type Conversion
```csharp
Console.WriteLine(Convert.ToDouble(num));    // 42
Console.WriteLine(Convert.ToString(str));    // "hello"
Console.WriteLine(Convert.ToBoolean(boolean)); // true
Console.WriteLine(Convert.ToString(undef));  // "undefined"
```

## API Reference
### JsValue
- Implicit Conversions: From int, double, string, bool, and JsLikeObject.
- Explicit Conversions: To double, int, bool, and string.
- Methods:
  - ToNumber(): Converts the value to a number.
  - ToBool(): Converts the value to a boolean.
  - ToJsString(): Returns a JavaScript-like string representation.
  - ToJsonString(): Returns a JSON string representation.

### JsArray
- Properties:
  - Length: Gets the number of elements in the array.

- Methods:
  - Push(JsValue value): Appends a value to the end of the array.

### JsObject
- Indexer: Access properties using obj["key"].

## License
This project is licensed under the MIT License - see the LICENSE file for details.

## Contributing
Contributions are welcome! Please open an issue or submit a pull request.
