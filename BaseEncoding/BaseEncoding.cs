/***************************************************************
• File: BaseEncoding.cs

• Description:

    It is an abstract class BaseEncoding, which is a descendant 
    of the Encoding class for creating various encodings.

• MIT License

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

namespace System.Text
{
    // BaseEncoding is an abstract class that serves as a base for various encoding implementations.
    public abstract class BaseEncoding : Encoding
    {
        // Constructor for BaseEncoding that takes a code page.
        protected BaseEncoding(int codePage) : base(codePage)
        {
        }

        // Factory method to get an encoding based on the provided alphabet.
        public static new BaseEncoding GetEncoding(string alphabet)
        {
            return alphabet.Length > CustomEncodingSmall.MAX_BASE
                ? new CustomEncodingBig(alphabet)
                : new CustomEncodingSmall(alphabet);
        }

        // Factory method to get an encoding based on the specified encoding style.
        public static BaseEncoding GetEncoding(BasesEncodingStyle encoding)
        {
            switch (encoding)
            {
                case BasesEncodingStyle.Binary:
                    return new Base2Encoding();
                case BasesEncodingStyle.Octal:
                    return new Base8Encoding();
                case BasesEncodingStyle.Hexadecimal:
                    return new Base16Encoding();
                case BasesEncodingStyle.Base32:
                    return new Base32Encoding();
                case BasesEncodingStyle.Base64:
                    return new Base64Encoding();
                default:
                    throw new ArgumentOutOfRangeException(nameof(encoding), encoding, null);
            }
        }

        // Abstract method to encode a character array into a byte array.
        public override int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex)
        {
            ValidateInput(chars, charIndex, charCount);
            ValidateOutput(bytes, byteIndex);

            return 0; // This method should be overridden to implement actual encoding logic.
        }

        // Abstract method to decode a byte array into a character array.
        public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex)
        {
            ValidateInput(bytes, byteIndex, byteCount);
            ValidateOutput(chars, charIndex);

            return 0; // This method should be overridden to implement actual decoding logic.
        }

        // Calculates the number of bytes needed to encode a character array.
        public override int GetByteCount(char[] chars, int index, int count)
        {
            ValidateInput(chars, index, count);

            return GetMaxByteCount(GetMaxCount(chars, index, count));
        }

        // Calculates the number of characters needed to decode a byte array.
        public override int GetCharCount(byte[] bytes, int index, int count)
        {
            ValidateInput(bytes, index, count);

            return GetMaxCharCount(GetMaxCount(bytes, index, count));
        }

        // Calculates the number of characters that can be obtained from a byte array.
        protected virtual int GetCharCount(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex)
        {
            ValidateInput(chars, charIndex, charCount);
            ValidateOutput(bytes, byteIndex);

            int maxCharCount = GetMaxCharCount(GetMaxCount(bytes, byteIndex));
            return Math.Min(GetMaxCount(chars, charIndex, charCount), maxCharCount);
        }

        // Calculates the number of bytes that can be obtained from a character array.
        protected virtual int GetByteCount(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex)
        {
            ValidateInput(bytes, byteIndex, byteCount);
            ValidateOutput(chars, charIndex);

            int maxByteCount = GetMaxByteCount(GetMaxCount(chars, charIndex));
            byteCount = Math.Min(GetMaxCount(bytes, byteIndex, byteCount), maxByteCount);
            return byteCount;
        }

        // Validates input array and parameters.
        protected internal static void ValidateInput<T>(T[] input, int index, int count)
        {
            ArgumentNullException.ThrowIfNull(input);
            ArgumentOutOfRangeException.ThrowIfNegative(index);
            ArgumentOutOfRangeException.ThrowIfNegative(count);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(index + count, input.Length, "index + count");
        }

        // Validates output buffer index.
        protected internal static void ValidateOutput<T>(T[] buffer, int index)
        {
            ArgumentNullException.ThrowIfNull(buffer);
            ArgumentOutOfRangeException.ThrowIfNegative(index);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(index, buffer.Length);
        }

        // Returns the maximum possible length to retrieve elements from an array, starting at a specified position.
        protected internal static int GetMaxCount<T>(T[] array, int startIndex, int count)
        {
            return Math.Min(count, array.Length - startIndex);
        }

        // Returns the maximum possible length to retrieve elements from an array, starting at a specified position.
        protected internal static int GetMaxCount<T>(T[] array, int startIndex)
        {
            return array.Length - startIndex;
        }

        // Throws an exception if decoding input data contains an invalid symbol.
        protected internal static Exception BadBaseException(char digit)
        {
            return new FormatException($"Invalid digit {digit} for the specified base.");
        }
    }
}
