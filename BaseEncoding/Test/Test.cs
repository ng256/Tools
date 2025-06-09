/***************************************************************

•   File: Test.cs

•   Description.

    BaseEncoding class test program.


•   MIT License

    Copyright © Pavel Bashkardin, 2024

    Permission is hereby granted, free of charge, to any person obtaining a copy
    of this software and associated documentation files (the "Software"), to deal
    in the Software without restriction, including without limitation the rights
    to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
    copies of the Software, and to permit persons to whom the Software is
    furnished to do so, subject to the following conditions:

    The above copyright notice and this permission notice shall be included in all
    copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
    SOFTWARE.


***************************************************************/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace System.Text
{
    public static class Program
    {
        public static void Main()
        {
            var tester = new BaseEncodingTester();
            tester.RunAllTests();
            tester.PrintSummary();
        }
    }

    public class BaseEncodingTester
    {
        private readonly Random _random = new Random();
        private readonly Stopwatch _stopwatch = new Stopwatch();
        private readonly List<TestResult> _results = new List<TestResult>();
        private readonly Dictionary<string, EncodingStats> _stats = new Dictionary<string, EncodingStats>();

        public void RunAllTests()
        {
            _stopwatch.Start();

            TestKnownVectors();
            TestRandomDataRoundTrip();
            TestCustomAlphabets();
            TestPaddingHandling();
            TestInvalidAlphabets();
            TestErrorConditions();
            TestEdgeCases();

            _stopwatch.Stop();
        }

        public void PrintSummary()
        {
            Console.WriteLine("\n=== Test Summary ===");
            Console.WriteLine($"Total Tests: {_results.Count}");
            Console.WriteLine($"Total Time: {_stopwatch.ElapsedMilliseconds}ms\n");

            // Group by encoding type
            var groupedStats = _stats.Values
                .GroupBy(s => s.Category)
                .OrderBy(g => g.Key);

            foreach (var group in groupedStats)
            {
                Console.WriteLine($"Category: {group.Key}");
                Console.WriteLine("------------------------------------------------");
                Console.WriteLine("Encoding               Tests  Passed  Failed  Success Rate");

                foreach (var stat in group.OrderBy(s => s.Name))
                {
                    double successRate = stat.Total > 0 ?
                        stat.Passed * 100.0 / stat.Total : 100;

                    Console.WriteLine($"{stat.Name,-20} {stat.Total,6} {stat.Passed,7} {stat.Failed,7} {successRate,12:0.00}%");
                }
                Console.WriteLine();
            }

            // Identify working/broken encodings
            Console.WriteLine("\n=== Encoding Status ===");
            Console.WriteLine("Fully Working Encodings:");
            foreach (var stat in _stats.Values.Where(s => s.Failed == 0).OrderBy(s => s.Name))
            {
                Console.WriteLine($"- {stat.Name}");
            }

            Console.WriteLine("\nEncodings with Failures:");
            foreach (var stat in _stats.Values.Where(s => s.Failed > 0).OrderBy(s => s.Name))
            {
                Console.WriteLine($"- {stat.Name} ({stat.Failed} failures)");
            }
        }

        private void RecordResult(string category, string encodingName, bool passed,
                                 string testCase, string errorMessage = null)
        {
            var result = new TestResult
            {
                Category = category,
                EncodingName = encodingName,
                Passed = passed,
                TestCase = testCase,
                ErrorMessage = errorMessage
            };

            _results.Add(result);

            // Update statistics
            string key = $"{category}-{encodingName}";
            if (!_stats.TryGetValue(key, out var stat))
            {
                stat = new EncodingStats
                {
                    Category = category,
                    Name = encodingName
                };
                _stats[key] = stat;
            }

            stat.Total++;
            if (passed) stat.Passed++;

            // Print failed tests immediately
            if (!passed)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[FAIL] {category} > {encodingName} > {testCase}");
                Console.WriteLine($"  Error: {errorMessage}");
                Console.ResetColor();
            }
        }

        private void TestKnownVectors()
        {
            var testCases = new[]
            {
                (Style: BaseEncodingStyle.Binary,
                 Data: new byte[]{0x48, 0x65, 0x6C, 0x6C, 0x6F},
                 Expected: "0100100001100101011011000110110001101111"),

                (Style: BaseEncodingStyle.Octal,
                 Data: new byte[]{0x48, 0x65, 0x6C, 0x6C, 0x6F},
                 Expected: "110145154154157"),

                (Style: BaseEncodingStyle.Hexadecimal,
                 Data: new byte[]{0x48, 0x65, 0x6C, 0x6C, 0x6F},
                 Expected: "48656C6C6F"),

                (Style: BaseEncodingStyle.Base32,
                 Data: new byte[]{0x48, 0x65, 0x6C, 0x6C, 0x6F},
                 Expected: "JBSWY3DP"),

                (Style: BaseEncodingStyle.Base64,
                 Data: new byte[]{0x48, 0x65, 0x6C, 0x6C, 0x6F},
                 Expected: "SGVsbG8")
            };

            foreach (var test in testCases)
            {
                string encodingName = test.Style.ToString();
                string testCase = "Known vector test";
                string error = null;
                bool passed = false;

                try
                {
                    var encoding = BaseEncoding.GetEncoding(test.Style);

                    // Test encoding
                    string encoded = encoding.GetString(test.Data);
                    if (encoded != test.Expected)
                    {
                        error = $"Encoding mismatch!\nExpected: {test.Expected}\nActual:   {encoded}";
                        RecordResult("Standard", encodingName, false, testCase, error);
                        continue;
                    }

                    // Test decoding
                    byte[] decoded = encoding.GetBytes(encoded);
                    if (!test.Data.SequenceEqual(decoded))
                    {
                        error = $"Decoding mismatch!\nExpected: {FormatBytes(test.Data)}\nActual:   {FormatBytes(decoded)}";
                        RecordResult("Standard", encodingName, false, testCase, error);
                        continue;
                    }

                    passed = true;
                }
                catch (Exception ex)
                {
                    error = $"Unexpected error: {ex.GetType().Name} - {ex.Message}";
                }

                RecordResult("Standard", encodingName, passed, testCase, error);
            }
        }

        private void TestRandomDataRoundTrip()
        {
            var sizes = new[] { 0, 1, 2, 3, 4, 5, 10, 50, 100, 500, 1024, 2048, 65535 };
            var styles = Enum.GetValues(typeof(BaseEncodingStyle)).Cast<BaseEncodingStyle>();

            foreach (var size in sizes)
            {
                byte[] data = GenerateRandomData(size);

                foreach (var style in styles)
                {
                    string encodingName = style.ToString();
                    string testCase = $"RoundTrip [{size} bytes]";
                    string error = null;
                    bool passed = false;

                    try
                    {
                        var encoding = BaseEncoding.GetEncoding(style);
                        string encoded = encoding.GetString(data);
                        byte[] decoded = encoding.GetBytes(encoded);

                        if (!data.SequenceEqual(decoded))
                        {
                            error = $"Data mismatch!\nOriginal: {FormatBytes(data)}\nDecoded:  {FormatBytes(decoded)}";
                        }
                        else
                        {
                            passed = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        error = $"Unexpected error: {ex.GetType().Name} - {ex.Message}";
                    }

                    RecordResult("Standard", encodingName, passed, testCase, error);
                }
            }
        }

        private void TestCustomAlphabets()
        {
            var alphabets = new[]
            {
                ("Base2", "01"),
                ("Base8", "01234567"),
                ("Base10", "0123456789"),
                ("Base16", "0123456789ABCDEF"),
                ("Base32", "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567"),
                ("Base64", "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/"),
                ("Base62", "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz"),
                ("PrintableASCII", "!\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~"),
                ("Japanese", "あいうえおかきくけこさしすせそたちつてとなにぬねのはひふへほまみむめもやゆよらりるれろわをん")
            };

            var sizes = new[] { 0, 1, 2, 3, 10, 100, 1024 };

            foreach (var (name, alphabet) in alphabets)
            {
                foreach (var size in sizes)
                {
                    string testCase = $"RoundTrip [{size} bytes]";
                    string error = null;
                    bool passed = false;
                    byte[] data = GenerateRandomData(size);

                    try
                    {
                        var encoding = BaseEncoding.GetEncoding(alphabet);
                        string encoded = encoding.GetString(data);
                        byte[] decoded = encoding.GetBytes(encoded);

                        if (!data.SequenceEqual(decoded))
                        {
                            error = $"Data mismatch!\nOriginal: {FormatBytes(data)}\nDecoded:  {FormatBytes(decoded)}";
                        }
                        else
                        {
                            passed = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        error = $"Unexpected error: {ex.GetType().Name} - {ex.Message}";
                    }

                    RecordResult("Custom", name, passed, testCase, error);
                }
            }
        }

        private void TestPaddingHandling()
        {
            var testCases = new[]
            {
                (Style: BaseEncodingStyle.Base32,
                 Unpadded: "JBSWY3DP",
                 Padded: "JBSWY3DP======",
                 Expected: new byte[]{0x48, 0x65, 0x6C, 0x6C, 0x6F}),

                (Style: BaseEncodingStyle.Base64,
                 Unpadded: "SGVsbG8",
                 Padded: "SGVsbG8=",
                 Expected: new byte[]{0x48, 0x65, 0x6C, 0x6C, 0x6F}),
            };

            foreach (var test in testCases)
            {
                string encodingName = test.Style.ToString();

                TestPaddingCase(encodingName, "Unpadded", test.Unpadded, test.Expected);
                TestPaddingCase(encodingName, "Padded", test.Padded, test.Expected);
            }
        }

        private void TestPaddingCase(string encodingName, string type, string input, byte[] expected)
        {
            string testCase = $"Padding handling ({type})";
            string error = null;
            bool passed = false;

            try
            {
                var encoding = BaseEncoding.GetEncoding(
                    Enum.Parse<BaseEncodingStyle>(encodingName));

                byte[] decoded = encoding.GetBytes(input);

                if (!expected.SequenceEqual(decoded))
                {
                    error = $"Decoding mismatch!\nExpected: {FormatBytes(expected)}\nActual:   {FormatBytes(decoded)}";
                }
                else
                {
                    passed = true;
                }
            }
            catch (Exception ex)
            {
                error = $"Unexpected error: {ex.GetType().Name} - {ex.Message}";
            }

            RecordResult("Standard", encodingName, passed, testCase, error);
        }

        private void TestInvalidAlphabets()
        {
            var invalidAlphabets = new[]
            {
                ("Empty", ""),
                ("Single char", "A"),
                ("Duplicate chars", "AABBCC"),
                ("With space", "ABC DEF"),
                ("With tab", "AB\tCD"),
                ("With newline", "AB\nCD"),
                ("With carriage return", "AB\rCD"),
                ("With control char", "AB" + (char)1 + "CD"),
                ("With non-printable", "AB\aCD\b"),
                ("With null char", "AB\0CD")
            };

            foreach (var (name, alphabet) in invalidAlphabets)
            {
                string testCase = $"Alphabet validation";
                string error = null;
                bool passed = false;

                try
                {
                    BaseEncoding.GetEncoding(alphabet);
                    error = "No exception thrown for invalid alphabet";
                }
                catch (ArgumentException)
                {
                    passed = true;
                }
                catch (Exception ex)
                {
                    error = $"Unexpected exception: {ex.GetType().Name}";
                }

                RecordResult("Validation", name, passed, testCase, error);
            }
        }

        private void TestErrorConditions()
        {
            // Invalid enum value
            TestThrows<ArgumentOutOfRangeException>(
                "Invalid enum value",
                () => BaseEncoding.GetEncoding((BaseEncodingStyle)100)
            );

            // Null alphabet
            TestThrows<ArgumentNullException>(
                "Null alphabet",
                () => BaseEncoding.GetEncoding((string)null)
            );

            // Test invalid characters
            TestInvalidCharacters(BaseEncodingStyle.Base64, "SGVsbG8!", "Invalid character");
            TestInvalidCharacters("01", "01021", "Invalid character in custom");
        }

        private void TestInvalidCharacters(BaseEncodingStyle style, string input, string testName)
        {
            string encodingName = style.ToString();
            string testCase = testName;
            string error = null;
            bool passed = false;

            try
            {
                var encoding = BaseEncoding.GetEncoding(style);
                encoding.GetBytes(input);
                error = "No exception thrown for invalid character";
            }
            catch (FormatException)
            {
                passed = true;
            }
            catch (Exception ex)
            {
                error = $"Unexpected exception: {ex.GetType().Name}";
            }

            RecordResult("Standard", encodingName, passed, testCase, error);
        }

        private void TestInvalidCharacters(string alphabet, string input, string testName)
        {
            string testCase = testName;
            string error = null;
            bool passed = false;

            try
            {
                var encoding = BaseEncoding.GetEncoding(alphabet);
                encoding.GetBytes(input);
                error = "No exception thrown for invalid character";
            }
            catch (FormatException)
            {
                passed = true;
            }
            catch (Exception ex)
            {
                error = $"Unexpected exception: {ex.GetType().Name}";
            }

            // Try to find a name for the custom alphabet
            string name = "Custom-" + alphabet;
            if (alphabet.Length > 10) name = "Custom-" + alphabet.Substring(0, 10) + "...";

            RecordResult("Custom", name, passed, testCase, error);
        }

        private void TestThrows<TException>(string testName, Action action) where TException : Exception
        {
            string error = null;
            bool passed = false;

            try
            {
                action();
                error = $"Expected {typeof(TException).Name} but no exception was thrown";
            }
            catch (TException)
            {
                passed = true;
            }
            catch (Exception ex)
            {
                error = $"Expected {typeof(TException).Name} but got {ex.GetType().Name}";
            }

            RecordResult("Validation", testName, passed, testName, error);
        }

        private void TestEdgeCases()
        {
            // Empty strings
            TestEmptyString(BaseEncodingStyle.Base64);
            TestEmptyString("01");
            TestSingleByte(BaseEncodingStyle.Base64);
            TestSingleByte("01");
        }

        private void TestEmptyString(BaseEncodingStyle style)
        {
            string encodingName = style.ToString();
            string testCase = "Empty string";
            string error = null;
            bool passed = false;

            try
            {
                var encoding = BaseEncoding.GetEncoding(style);
                byte[] emptyBytes = Array.Empty<byte>();

                string encoded = encoding.GetString(emptyBytes);
                byte[] decoded = encoding.GetBytes(encoded);

                if (encoded != string.Empty || !emptyBytes.SequenceEqual(decoded))
                {
                    error = $"Empty string handling failed";
                }
                else
                {
                    passed = true;
                }
            }
            catch (Exception ex)
            {
                error = $"Unexpected error: {ex.GetType().Name} - {ex.Message}";
            }

            RecordResult("Standard", encodingName, passed, testCase, error);
        }

        private void TestEmptyString(string alphabet)
        {
            string name = "Custom-" + alphabet;
            if (alphabet.Length > 10) name = "Custom-" + alphabet.Substring(0, 10) + "...";

            string testCase = "Empty string";
            string error = null;
            bool passed = false;

            try
            {
                var encoding = BaseEncoding.GetEncoding(alphabet);
                byte[] emptyBytes = Array.Empty<byte>();

                string encoded = encoding.GetString(emptyBytes);
                byte[] decoded = encoding.GetBytes(encoded);

                if (encoded != string.Empty || !emptyBytes.SequenceEqual(decoded))
                {
                    error = $"Empty string handling failed";
                }
                else
                {
                    passed = true;
                }
            }
            catch (Exception ex)
            {
                error = $"Unexpected error: {ex.GetType().Name} - {ex.Message}";
            }

            RecordResult("Custom", name, passed, testCase, error);
        }

        private void TestSingleByte(BaseEncodingStyle style)
        {
            string encodingName = style.ToString();
            string testCase = "Single byte";
            string error = null;
            bool passed = false;

            try
            {
                var encoding = BaseEncoding.GetEncoding(style);
                byte[] data = { 0x41 };

                string encoded = encoding.GetString(data);
                byte[] decoded = encoding.GetBytes(encoded);

                if (!data.SequenceEqual(decoded))
                {
                    error = $"Single byte handling failed";
                }
                else
                {
                    passed = true;
                }
            }
            catch (Exception ex)
            {
                error = $"Unexpected error: {ex.GetType().Name} - {ex.Message}";
            }

            RecordResult("Standard", encodingName, passed, testCase, error);
        }

        private void TestSingleByte(string alphabet)
        {
            string name = "Custom-" + alphabet;
            if (alphabet.Length > 10) name = "Custom-" + alphabet.Substring(0, 10) + "...";

            string testCase = "Single byte";
            string error = null;
            bool passed = false;

            try
            {
                var encoding = BaseEncoding.GetEncoding(alphabet);
                byte[] data = { 0x41 };

                string encoded = encoding.GetString(data);
                byte[] decoded = encoding.GetBytes(encoded);

                if (!data.SequenceEqual(decoded))
                {
                    error = $"Single byte handling failed";
                }
                else
                {
                    passed = true;
                }
            }
            catch (Exception ex)
            {
                error = $"Unexpected error: {ex.GetType().Name} - {ex.Message}";
            }

            RecordResult("Custom", name, passed, testCase, error);
        }

        private byte[] GenerateRandomData(int size)
        {
            if (size == 0) return Array.Empty<byte>();

            byte[] data = new byte[size];
            _random.NextBytes(data);
            return data;
        }

        private static string FormatBytes(byte[] data)
        {
            if (data == null) return "null";
            if (data.Length == 0) return "Empty";

            const int maxLength = 20;
            if (data.Length <= maxLength)
            {
                return BitConverter.ToString(data);
            }

            return $"{BitConverter.ToString(data, 0, maxLength / 2)}..." +
                   $"{BitConverter.ToString(data, data.Length - maxLength / 2, maxLength / 2)} " +
                   $"[{data.Length} bytes]";
        }
    }

    // =========================================
    // Supporting Types
    // =========================================

    public class TestResult
    {
        public string Category { get; set; }
        public string EncodingName { get; set; }
        public bool Passed { get; set; }
        public string TestCase { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class EncodingStats
    {
        public string Category { get; set; }
        public string Name { get; set; }
        public int Total { get; set; }
        public int Passed { get; set; }
        public int Failed => Total - Passed;
    }
}