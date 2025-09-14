class Program
{
    private static string executableName = System.IO.Path.GetFileName(System.Reflection.Assembly.GetEntryAssembly().Location);

    static void Main(string[] args)
    {
        // Обработка аргументов командной строки
        string inputFile = null;
        string outputFile = null;
        bool prettyPrint = true;
        string query = null;
        bool runTests = false;
        bool showHelp = false;

        // Парсинг аргументов командной строки
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--file":
                case "-f":
                    if (i + 1 < args.Length) inputFile = args[++i];
                    break;
                case "--out":
                case "-o":
                    if (i + 1 < args.Length) outputFile = args[++i];
                    break;
                case "--pretty":
                case "-p":
                    if (i + 1 < args.Length) 
                        bool.TryParse(args[++i], out prettyPrint);
                    else
                        prettyPrint = true;
                    break;
                case "--no-pretty":
                    prettyPrint = false;
                    break;
                case "--test":
                    runTests = true;
                    break;
                case "--help":
                case "-h":
                    showHelp = true;
                    break;
                default:
                    // Если аргумент не начинается с -, считаем его запросом
                    if (!args[i].StartsWith("-") && query == null)
                        query = args[i];
                    break;
            }
        }

#if DEBUG
        if (runTests)
        {
            Test();
            return;
        }
#endif

        if (showHelp)
        {
            ShowHelp();
            return;
        }

        // Чтение входных данных
        string input = ReadInput(inputFile);
        if (string.IsNullOrEmpty(input))
        {
            Console.WriteLine("No input provided. Use --help for usage information.");
            return;
        }

        try
        {
            // Парсинг JSON
            JsValue jsonValue = JsValue.FromJsonString(input);

            // Обработка запроса
            JsValue result;
            if (!string.IsNullOrEmpty(query))
            {
                // Проверяем, является ли запрос путём для выборки
                if (query.Contains("/") || query.Contains("*") || query.Contains("["))
                {
                    // Используем SelectMany для запросов с wildcard или путём
                    var results = jsonValue.SelectMany(query).ToList();
                    
                    if (results.Count == 1)
                    {
                        result = results[0];
                    }
                    else if (results.Count > 1)
                    {
                        // Если результатов несколько, выводим как массив
                        result = JsValue.FromArray(results.ToArray());
                    }
                    else
                    {
                        result = JsUndefined.Instance;
                    }
                }
                else
                {
                    // Простой запрос к свойству
                    result = jsonValue.Select(query);
                }
            }
            else
            {
                // Если запрос не указан, используем исходный JSON
                result = jsonValue;
            }

            // Форматирование и вывод результата
            string output = result.ToJsonString(prettyPrint);
            
            if (!string.IsNullOrEmpty(outputFile))
            {
                System.IO.File.WriteAllText(outputFile, output);
                Console.WriteLine($"Output written to: {outputFile}");
            }
            else
            {
                Console.WriteLine(output);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            Environment.Exit(1);
        }
    }

    static string ReadInput(string inputFile)
    {
        if (!string.IsNullOrEmpty(inputFile))
        {
            // Чтение из файла
            if (!System.IO.File.Exists(inputFile))
            {
                Console.WriteLine($"File not found: {inputFile}");
                Environment.Exit(1);
            }
            return System.IO.File.ReadAllText(inputFile);
        }
        
        // Проверяем, есть ли данные в stdin
        if (Console.IsInputRedirected)
        {
            StringBuilder sb = new StringBuilder();
            string line;
            while ((line = Console.ReadLine()) != null)
            {
                sb.AppendLine(line);
            }
            return sb.ToString();
        }
        else
        {
            // Используем полнофункциональный редактор для ввода с клавиатуры
            Console.WriteLine("Enter JSON (press Ctrl+C to finish):");
            return ConsoleEditor.ReadAllLines();
        }
    }

    static void ShowHelp()
    {
        Console.WriteLine($"{executableName} - JSON Processor with JavaScript-like semantics");
        Console.WriteLine();
        Console.WriteLine("Usage:");
        Console.WriteLine($"  {executableName} [query] [options]");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  -f, --file <file>    Read input from file instead of stdin");
        Console.WriteLine("  -o, --out <file>     Write output to file instead of stdout");
        Console.WriteLine("  -p, --pretty <bool>  Enable/disable pretty printing (default: true)");
        Console.WriteLine("  --no-pretty          Disable pretty printing (same as --pretty false)");
        Console.WriteLine("  -h, --help           Show this help message");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine($"  {executableName}");
        Console.WriteLine($"  {executableName} \"name\"");
        Console.WriteLine($"  {executableName} \"users/*/name\" -f input.json");
        Console.WriteLine($"  {executableName} -f input.json -o output.json --no-pretty");
        Console.WriteLine($"  echo '{{\"name\": \"John\"}}' | {executableName}");
        Console.WriteLine();
        Console.WriteLine("Query syntax:");
        Console.WriteLine("  property      - Access object property");
        Console.WriteLine("  path/to/prop  - Access nested property");
        Console.WriteLine("  array/[0]     - Access array element by index");
        Console.WriteLine("  array/*       - Access all array elements (wildcard)");
        Console.WriteLine("  *             - Access all properties/elements");
        Console.WriteLine();
        Console.WriteLine("Features:");
        Console.WriteLine("  - Supports single-line (//) and multi-line (/* */) comments in JSON");
        Console.WriteLine("  - JavaScript-like type coercion and truthy/falsy evaluation");
        Console.WriteLine("  - XPath-like queries for nested object navigation");
        Console.WriteLine("  - Interactive editor with syntax highlighting when no input provided");
    }

#if DEBUG
    static void Test()
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

            Console.WriteLine("\nAll tests completed successfully!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Test error: {ex.Message}");
        }
    }
#endif
}
