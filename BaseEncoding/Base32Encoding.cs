/***************************************************************************************

• File: Base32Encoding.cs

• Description:

    Base32Encoding implements methods for encoding and decoding data using Base32 
    encoding.

•   MIT License

Copyright © Pavel Bashkardin, 2024

Permission  is  hereby  granted,  free  of  charge,  to  any  person  obtaining  a  copy
of  this  software  and  associated  documentation  files  (the  "Software"),  to  deal
in  the  Software  without  restriction,  including  without  limitation  the  rights to
use,  copy,  modify,  merge,  publish,  distribute,  sublicense,  and/or  sell copies of
the  Software,  and  to  permit  persons  to  whom  the  Software  is  furnished  to  do
so, subject to the following conditions:

The  above  copyright  notice  and  this  permission  notice  shall  be  included in all
copies or substantial portions of the Software.

THE  SOFTWARE  IS  PROVIDED  "AS  IS",  WITHOUT  WARRANTY  OF  ANY  KIND,  EXPRESS   OR
IMPLIED,  INCLUDING  BUT  NOT  LIMITED  TO  THE  WARRANTIES  OF  MERCHANTABILITY, FITNESS
FOR  A  PARTICULAR  PURPOSE  AND  NONINFRINGEMENT.  IN  NO  EVENT  SHALL  THE  AUTHORS OR
COPYRIGHT  HOLDERS  BE  LIABLE  FOR  ANY  CLAIM,  DAMAGES  OR  OTHER  LIABILITY,  WHETHER
IN  AN  ACTION  OF  CONTRACT,  TORT  OR  OTHERWISE,  ARISING  FROM,  OUT  OF  OR   IN
CONNECTION  WITH  THE  SOFTWARE  OR  THE  USE  OR  OTHER  DEALINGS  IN  THE  SOFTWARE.

****************************************************************************************/

namespace System.Text
{
    // Base32Encoding class provides custom Base32 encoding and decoding functionality.
    internal sealed class Base32Encoding : BaseEncoding
    {
        public override string EncodingName => "base-32";

        public Base32Encoding() : base(0)
        {
        }

        // Encodes a string into a byte array using Base32 encoding.
        public override byte[] GetBytes(string s)
        {
            return base.GetBytes(s.Trim('='));
        }

        // Decodes a byte array into a character array using Base32 encoding.
        public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex)
        {
            int startCharIndex = charIndex;

            int fullBlocks = byteCount / 5;  // Each 5 bytes -> 8 chars
            int remainder = byteCount % 5;

            for (int i = 0; i < fullBlocks; i++)
            {
                // Compose 40 bits from 5 bytes into 64-bit buffer
                ulong buffer = ((ulong)bytes[byteIndex++] << 32) |
                               ((ulong)bytes[byteIndex++] << 24) |
                               ((ulong)bytes[byteIndex++] << 16) |
                               ((ulong)bytes[byteIndex++] << 8) |
                               (ulong)bytes[byteIndex++];

                // Extract 8 groups of 5 bits and convert to chars
                chars[charIndex++] = GetDigit((int)((buffer >> 35) & 0x1F));
                chars[charIndex++] = GetDigit((int)((buffer >> 30) & 0x1F));
                chars[charIndex++] = GetDigit((int)((buffer >> 25) & 0x1F));
                chars[charIndex++] = GetDigit((int)((buffer >> 20) & 0x1F));
                chars[charIndex++] = GetDigit((int)((buffer >> 15) & 0x1F));
                chars[charIndex++] = GetDigit((int)((buffer >> 10) & 0x1F));
                chars[charIndex++] = GetDigit((int)((buffer >> 5) & 0x1F));
                chars[charIndex++] = GetDigit((int)(buffer & 0x1F));
            }

            if (remainder > 0)
            {
                // Handle remaining bytes (less than 5)
                ulong buffer = 0;
                for (int i = 0; i < remainder; i++)
                {
                    buffer |= ((ulong)bytes[byteIndex + i]) << (32 - 8 * i);
                }

                // Calculate how many Base32 chars correspond to remainder bytes
                // Formula: ceil((remainder * 8) / 5)
                int outputChars = (remainder * 8 + 4) / 5;

                for (int i = 0; i < outputChars; i++)
                {
                    int shift = 35 - 5 * i;
                    chars[charIndex++] = GetDigit((int)((buffer >> shift) & 0x1F));
                }
            }

            return charIndex - startCharIndex;
        }

        // Encodes a character array into a byte array using Base32 encoding.
        public override int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex)
        {
            int startByteIndex = byteIndex;
            int endCharIndex = charIndex + charCount;

            uint buffer = 0;
            int bits = 0;

            while (charIndex < endCharIndex)
            {
                char c = chars[charIndex++];
                if (c == '=') continue; // Ignore padding

                buffer = (buffer << 5) | (uint)GetValue(c);
                bits += 5;

                if (bits >= 8)
                {
                    bits -= 8;
                    bytes[byteIndex++] = (byte)(buffer >> bits);
                    buffer &= (uint)((1 << bits) - 1);
                }
            }

            return byteIndex - startByteIndex;
        }

        // Converts a value to its corresponding Base32 character.
        public char GetDigit(int value)
        {
            if (value < 0x1A) return (char)(value + 'A');
            if (value < 0x20) return (char)(value - 0x1A + '2');
            return '=';
        }

        // Converts a Base32 character to its corresponding value.
        private int GetValue(char digit)
        {
            if (digit >= 'A' && digit <= 'Z') return digit - 'A';
            if (digit >= 'a' && digit <= 'z') return digit - 'a';
            if (digit >= '2' && digit <= '7') return digit - '2' + 0x1A;
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
            return (((charCount << 2) + charCount)) >> 3;
        }

        // Calculates the maximum number of characters needed to decode a given number of bytes.
        public override int GetMaxCharCount(int byteCount)
        {
            return ((byteCount << 3) + 4) / 5;
        }

        // Creates a shallow copy of the current Base32Encoding object.
        public override object Clone()
        {
            return new Base32Encoding();
        }

        private Exception BadBaseException(char digit)
        {
            return new ArgumentException($"Invalid Base32 character: {digit}");
        }
    }
}