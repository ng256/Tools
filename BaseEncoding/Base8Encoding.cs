/*************************************************************** 

•   File: Base8Encoding.cs

•   Description.

    Base8Encoding is designed to work with octal data and
    implements methods for encoding and decoding data.

•   Copyright

    © Pavel Bashkardin, 2022-2024

***************************************************************/

using static System.InternalTools;

namespace System.Text
{
    internal sealed class Base8Encoding : BaseEncoding
    {
        // Returns the name of the encoding
        public override string EncodingName => "base-8";

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
            throw new ArgumentOutOfRangeException(nameof(digit), digit, GetResourceString("Format_BadBase"));
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
