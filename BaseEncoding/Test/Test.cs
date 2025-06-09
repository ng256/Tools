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

using System.Text;

namespace CustomEncodingTest
{
    class Test
    {
        static void Main()
        {
            Console.WriteLine("CustomEncoding Test Application");
            Console.WriteLine("===============================\n");
            
            TestRunner.RunTests();
            
            Console.WriteLine("\nTesting completed. Press any key to exit...");
            Console.ReadKey();
        }
    }

    public static class TestRunner
    {
        private const int MAX_ERRORS = 10;
        private static readonly int[] TEST_SIZES = {
            0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10,
            15, 16, 17, 31, 32, 33, 63, 64, 65,
            127, 128, 129, 255, 256, 257, 511, 512, 1023,
            //1024, 4095, 4096, 8191, 8192, 16383, 16384, 32768,
            //65535, 65536, 131072, 262144, 524288, 1048576
        };

        private static readonly string[] TEST_ALPHABETS = {
            // Base64 alphabet
            "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/",
            
            // Base32 alphabet (RFC 4648)
            "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567",
            
            // Hexadecimal alphabet
            "0123456789ABCDEF",
            
            // Custom alphabet 1 (printable ASCII)
            "!\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~",
            
            // Custom alphabet 2 (Japanese characters)
            "あいうえおかきくけこさしすせそたちつてとなにぬねのはひふへほまみむめもやゆよらりるれろわをん",
            
            // Custom alphabet 3 (emoji)
            //"😀😃😄😁😆😅😂🤣🥲😊😇🙂🙃😉😌😍🥰😘😗😙😚😋😛😝😜🤪🤨🧐🤓😎🥸🤩🥳😏😒"
        };

        public static void RunTests()
        {
            int totalTests = 0;
            int failedTests = 0;
            var rnd = new Random(DateTime.Now.Millisecond);

            foreach (string alphabet in TEST_ALPHABETS)
            {
                Console.WriteLine($"\nTesting alphabet: {alphabet.Substring(0, Math.Min(20, alphabet.Length))}... ({alphabet.Length} chars)");
                Console.WriteLine("===============================================");

                var encoding = BaseEncoding.GetEncoding(alphabet);
                
                foreach (int size in TEST_SIZES)
                {
                    // Generate test data
                    byte[] original = GenerateTestData(rnd, size);
                    totalTests++;

                    // Encode and decode
                    byte[] decoded = Array.Empty<byte>();
                    string encoded = "";
                    
                    try
                    {
                        // Encode
                        encoded = encoding.GetString(original);

                        // Decode
                        decoded = encoding.GetBytes(encoded);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"\n❌ Size {size} bytes: FAILED ({ex.GetType().Name})");
                        Console.WriteLine($"Alphabet: {alphabet}");
                        Console.WriteLine($"Original: {BitConverter.ToString(original).Replace("-", "")}");
                        Console.WriteLine($"Encoded : {encoded}");
                        Console.WriteLine($"Decoded : {BitConverter.ToString(decoded).Replace("-", "")}");
                        Console.WriteLine($"Exception: {ex.Message}");
                        failedTests++;
                        if (failedTests >= MAX_ERRORS) return;
                        continue;
                    }

                    // Validate results
                    bool isValid = original.SequenceEqual(decoded);



                    if (!isValid)
                    {
                        failedTests++;
                        Console.WriteLine($"\nFailure details for size {size}:");
                        Console.WriteLine($"Alphabet: {alphabet}");
                        Console.WriteLine($"Original: {BitConverter.ToString(original).Replace("-", "")}");
                        Console.WriteLine($"Encoded : {encoded}");
                        Console.WriteLine($"Decoded : {BitConverter.ToString(decoded).Replace("-", "")}");
                        
                        if (failedTests >= MAX_ERRORS)
                        {
                            Console.WriteLine("\n⚠️ Too many errors. Stopping tests...");
                            break;
                        }
                    }
                }

                if (failedTests >= MAX_ERRORS) break;
            }

            Console.WriteLine("\nTest Summary");
            Console.WriteLine("============");
            Console.WriteLine($"Total tests : {totalTests}");
            Console.WriteLine($"Failed tests: {failedTests}");
            Console.WriteLine($"Success rate: {100 * (totalTests - failedTests) / (double)totalTests:0.00}%");
        }

        private static byte[] GenerateTestData(Random rnd, int size)
        {
            if (size == 0) return Array.Empty<byte>();
            
            byte[] data = new byte[size];
            rnd.NextBytes(data);
            
            // Special patterns for edge cases
            if (size > 4)
            {
                // All zeros
                data.AsSpan(0, size / 4).Fill(0);
                
                // All ones
                data.AsSpan(size / 4, size / 4).Fill(0xFF);
                
                // Alternating bits
                for (int i = size / 2; i < size; i++)
                {
                    data[i] = (byte)(i % 2 == 0 ? 0xAA : 0x55);
                }
            }
            
            return data;
        }
    }
}