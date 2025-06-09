/*************************************************************** 

•   File: Base8Encoding.cs

•   Description.

    Base8Encoding is designed to work with octal data and
    implements methods for encoding and decoding data.


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

namespace System.Text
{
    internal sealed class Base8Encoding : BaseEncoding
    {
        // Returns the name of the encoding
        public override string EncodingName => "octal";

        // Initialize a new instance of the Base8Encoding.
        public Base8Encoding()
            : base(0)
        {
        }

        // Encodes a set of characters into a byte array.
        public override int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex)
        {
            // Validate the input parameters.
            base.GetBytes(chars, charIndex, charCount, bytes, byteIndex);

            int startByteIndex = byteIndex;
            int endCharIndex = charIndex + charCount;

            // Encode each group of 3 characters into a byte.
            while (charIndex < endCharIndex)
            {
                int value = GetValue(chars[charIndex++]) << 6;

                if (charIndex < endCharIndex)
                    value |= GetValue(chars[charIndex++]) << 3;

                if (charIndex < endCharIndex)
                    value |= GetValue(chars[charIndex++]);

                bytes[byteIndex++] = (byte)value;
            }

            return byteIndex - startByteIndex;
        }

        // Decodes a byte array into a set of characters.
        public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex)
        {
            // Validate the input parameters.
            base.GetChars(bytes, byteIndex, byteCount, chars, charIndex);

            int startCharIndex = charIndex;
            int endByteIndex = byteIndex + byteCount;

            // Decode each byte into a group of 3 characters.
            while (byteIndex < endByteIndex)
            {
                byte value = bytes[byteIndex++];
                chars[charIndex++] = GetDigit((value >> 6) & 0x7);
                chars[charIndex++] = GetDigit((value >> 3) & 0x7);
                chars[charIndex++] = GetDigit(value & 0x7);
            }

            return charIndex - startCharIndex;
        }

        // Returns the maximum number of bytes required to encode a given number of characters.
        public override int GetMaxByteCount(int charCount)
        {
            return (charCount + 2) / 3; // Each group of 3 characters produces 1 byte.
        }

        // Returns the maximum number of characters required to decode a given number of bytes.
        public override int GetMaxCharCount(int byteCount)
        {
            return byteCount * 3; // Each byte produces 3 characters.
        }

        // Converts an octal character to its corresponding integer value.
        private static int GetValue(char digit)
        {
            if (digit >= 0x31 && digit <= 0x37)
                return digit - 0x30;
            throw BadBaseException(digit);
        }

        // Converts an integer value to its corresponding octal character.
        private static char GetDigit(int value)
        {
            return (char)(value + 0x30);
        }

        // Creates another instance of the Base8Encoding.
        public override object Clone()
        {
            return new Base8Encoding();
        }
    }
}
