/***************************************************************

• File: Base64Encoding.cs

• Description:

    Base64Encoding implements methods for encoding and decoding
    data using Base64 encoding. To make the text easier to read,
    the trailing symbol '=' were removed.

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
    // Base64Encoding class provides custom Base64 encoding and decoding functionality.
    internal sealed class Base64Encoding : BaseEncoding
    {
        public override string EncodingName => "base-64";

        public Base64Encoding() : base(0)
        {
        }

        // Encodes a string into a byte array using Base64 encoding.
        public override byte[] GetBytes(string s)
        {
            return base.GetBytes(s.Trim('='));
        }

        // Decodes a byte array into a character array using Base64 encoding.
        public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex)
        {
            // Validate the input parameters.
            base.GetChars(bytes, byteIndex, byteCount, chars, charIndex);

            int mod = byteCount % 3;
            int startCharIndex = charIndex;
            int endByteIndex = byteIndex + (byteCount - mod);

            // Process the bytes in blocks of 3 and convert them into 4 characters.
            while (byteIndex < endByteIndex)
            {
                chars[charIndex] = GetDigit((bytes[byteIndex] & 0xFC) >> 2);
                chars[charIndex + 1] = GetDigit((bytes[byteIndex] & 0x03) << 4 | (bytes[byteIndex + 1] & 0xF0) >> 4);
                chars[charIndex + 2] = GetDigit((bytes[byteIndex + 1] & 0xF) << 2 | (bytes[byteIndex + 2] & 0xC0) >> 6);
                chars[charIndex + 3] = GetDigit(bytes[byteIndex + 2] & 0x3F);
                byteIndex += 3;
                charIndex += 4;
            }

            // Handle the remaining bytes that do not form a complete block of 3.
            switch (mod)
            {
                case 1:
                    chars[charIndex] = GetDigit((bytes[endByteIndex] & 0xFC) >> 2);
                    chars[charIndex + 1] = GetDigit((bytes[endByteIndex] & 0x3) << 4);
                    charIndex += 2;
                    break;
                case 2:
                    chars[charIndex] = GetDigit((bytes[endByteIndex] & 0xFC) >> 2);
                    chars[charIndex + 1] = GetDigit((bytes[endByteIndex] & 0x3) << 4 | (bytes[endByteIndex + 1] & 0xF0) >> 4);
                    chars[charIndex + 2] = GetDigit((bytes[endByteIndex + 1] & 0xF) << 2);
                    charIndex += 3;
                    break;
            }

            return charIndex - startCharIndex;
        }

        // Encodes a character array into a byte array using Base64 encoding.
        public override int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex)
        {
            base.GetBytes(chars, charIndex, charCount, bytes, byteIndex);

            int mod = GetMaxByteCount(charCount) % 3;
            int startByteIndex = byteIndex;
            int endCharIndex = charIndex + charCount;
            uint block = byte.MaxValue;

            // Process the characters in blocks of 4 and convert them into 3 bytes.
            while (charIndex < endCharIndex)
            {
                uint value = (uint)GetValue(chars[charIndex++]);
                block = block << 6 | value;

                if ((block & 0x80000000U) != 0)
                {
                    bytes[byteIndex] = (byte)(block >> 16);
                    bytes[byteIndex + 1] = (byte)(block >> 8);
                    bytes[byteIndex + 2] = (byte)block;

                    byteIndex += 3;
                    block = 0xFF;
                }
            }

            // Handle the remaining characters that do not form a complete block of 4.
            switch (mod)
            {
                case 1:
                    bytes[byteIndex] = (byte)(block >> 4);
                    byteIndex += 1;
                    break;
                case 2:
                    bytes[byteIndex] = (byte)(block >> 10);
                    bytes[byteIndex + 1] = (byte)(block >> 2);
                    byteIndex += 2;
                    break;
            }

            return byteIndex - startByteIndex;
        }

        // Converts a value to its corresponding Base64 character.
        public char GetDigit(int value)
        {
            if (value < 0x1A) return (char)(value + 0x41);
            if (value < 0x34) return (char)(value + 0x47);
            if (value < 0x3E) return (char)(value - 0x04);
            if (value == 0x3E) return (char)0x2B;
            if (value == 0x3F) return (char)0x2F;
            return (char)0x3D;
        }

        // Converts a Base64 character to its corresponding value.
        private int GetValue(char digit)
        {
            if (digit < 0x5B && digit > 0x40) return digit - 0x41;
            if (digit < 0x7B && digit > 0x60) return digit - 0x47;
            if (digit < 0x3A && digit > 0x2F) return digit + 0x04;
            if (digit == 0x2B) return 0x3E;
            if (digit == 0x2F) return 0x3F;
            throw BadBaseException(digit);
        }

        // Calculates the number of bytes needed to encode a character array.
        public override int GetByteCount(char[] chars, int index, int count)
        {
            return GetMaxByteCount(chars.Length - index);
        }

        // Calculates the number of characters needed to decode a byte array.
        public override int GetCharCount(byte[] bytes, int index, int count)
        {
            return GetMaxCharCount(bytes.Length - index);
        }

        // Calculates the maximum number of bytes needed to encode a given number of characters.
        public override int GetMaxByteCount(int charCount)
        {
            return (charCount * 3) >> 2;
        }

        // Calculates the maximum number of characters needed to decode a given number of bytes.
        public override int GetMaxCharCount(int byteCount)
        {
            return ((byteCount << 2) | 2) / 3;
        }

        // Creates a shallow copy of the current Base64Encoding object.
        public override object Clone()
        {
            return new Base64Encoding();
        }
    }
}
