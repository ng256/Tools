/***************************************************************

• File: Base32Encoding.cs

• Description:

    Base32Encoding implements methods for encoding and decoding
    data using Base32 encoding.

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
    // Base32Encoding class provides custom Base32 encoding and decoding functionality.
    internal sealed class Base32Encoding : BaseEncoding
    {
        public override string EncodingName => "base-32";

        public Base32Encoding() : base(0)
        {
        }

        // Decodes a byte array into a character array using Base32 encoding.
        public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex)
        {
            // Validate the input parameters.
            base.GetChars(bytes, byteIndex, byteCount, chars, charIndex);

            int maxCharCount = GetMaxCharCount(byteCount);
            byte value = 0;
            byte bitsCount = 5;
            int startCharIndex = charIndex;
            int endByteIndex = byteIndex + byteCount;

            // Process each byte in the input array.
            while (byteIndex < endByteIndex)
            {
                byte currentByte = bytes[byteIndex++];
                chars[charIndex++] = GetDigit((byte)(value | (uint)currentByte >> 8 - bitsCount));

                // Handle the bits to form a complete Base32 character.
                if (bitsCount < 4)
                {
                    chars[charIndex++] = GetDigit((byte)(currentByte >> 3 - bitsCount & 0x1F));
                    bitsCount += 5;
                }

                bitsCount -= 3;
                value = (byte)(currentByte << bitsCount & 0x1F);
            }

            // Handle any remaining bits that form a partial character.
            if (charIndex != maxCharCount)
                chars[charIndex++] = GetDigit(value);

            return charIndex - startCharIndex;
        }

        // Encodes a character array into a byte array using Base32 encoding.
        public override int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex)
        {
            base.GetBytes(chars, charIndex, charCount, bytes, byteIndex);

            charCount = GetCharCount(chars, charIndex, charCount, bytes, byteIndex);
            int byteCount = GetMaxCount(bytes, byteIndex);
            int startByteIndex = byteIndex;
            int endCharIndex = charIndex + charCount;
            uint block = 0;
            byte shift = 8;

            // Process each character in the input array.
            while (charIndex < endCharIndex)
            {
                int value = GetValue(chars[charIndex++]);

                // Accumulate bits to form a complete byte.
                if (shift > 5)
                {
                    block |= (byte)(value << shift - 5);
                    shift -= 5;
                }
                else
                {
                    bytes[byteIndex++] = (byte)(block | (uint)(value >> 5 - shift));
                    block = (byte)(value << 3 + shift);
                    shift += 3;
                }
            }

            // Handle any remaining bits that form a partial byte.
            if (byteIndex != byteCount)
                bytes[byteIndex] = (byte)block;

            return byteIndex - startByteIndex;
        }

        // Converts a value to its corresponding Base32 character.
        private static char GetDigit(byte value)
        {
            return value < 0x1A ? (char)(value + 0x41) : (char)(value + 0x18);
        }

        // Converts a Base32 character to its corresponding value.
        private static int GetValue(char digit)
        {
            if (digit < 0x5B && digit > 0x40) return digit - 0x41;
            if (digit < 0x7B && digit > 0x60) return digit - 0x61;
            if (digit < 0x38 && digit > 0x31) return digit - 0x18;
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
            return (charCount << 2) + charCount + 0xB >> 3;
        }

        // Calculates the maximum number of characters needed to decode a given number of bytes.
        public override int GetMaxCharCount(int byteCount)
        {
            return (++byteCount << 3) / 5;
        }

        // Creates a shallow copy of the current Base32Encoding object.
        public override object Clone()
        {
            return new Base32Encoding();
        }
    }
}
