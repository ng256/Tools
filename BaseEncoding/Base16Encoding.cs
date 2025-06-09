/***************************************************************************************

•   File: Base16Encoding.cs

•   Description.

    Base16Encoding is designed to work with hexadecimal data and implements methods 
    for encoding and decoding data.


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
    // Implements the hexadecimal encoding.
    internal sealed class Base16Encoding : BaseEncoding
    {
        // Returns the name of the encoding
        public override string EncodingName => "hex";

        // Initialize a new instance of the Base16Encoding.
        public Base16Encoding()
          : base(0)
        {
        }

        // Encodes a set of characters into a byte array.
        public override int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex)
        {
            // Validate the input parameters.
            base.GetBytes(chars, charIndex, charCount, bytes, byteIndex);
            charCount = GetCharCount(chars, charIndex, charCount, bytes, byteIndex);
            int startByteIndex = byteIndex;
            int endCharIndex = charIndex + charCount;

            // Encode first character if the number of characters is odd.
            if (charCount % 2 > 0)
                bytes[byteIndex++] = (byte)GetValue(chars[charIndex]);

            // Encode each character pair into a byte.
            while (charIndex < endCharIndex)
            {
                int highBits = GetValue(chars[charIndex++]) << 4;
                int lowBits = GetValue(chars[charIndex++]);
                bytes[byteIndex++] = (byte)(highBits + lowBits);
            }

            return byteIndex - startByteIndex;
        }

        // Decodes a byte array into a set of characters.
        public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex)
        {
            // Validate the input parameters.
            base.GetChars(bytes, byteIndex, byteCount, chars, charIndex);
            byteCount = GetByteCount(bytes, byteIndex, byteCount, chars, charIndex);
            int startCharIndex = charIndex;
            int endByteIndex = byteIndex + byteCount;
            while (byteIndex < endByteIndex)
            {
                // Decode each byte into a pair of characters.
                byte value = bytes[byteIndex++];
                chars[charIndex++] = GeDigit(value / 16);
                chars[charIndex++] = GeDigit(value % 16);
            }
            return charIndex - startCharIndex;
        }

        // Returns the maximum number of bytes required to encode a given number of characters.
        public override int GetMaxByteCount(int charCount)
        {
            return charCount / 2;
        }

        // Returns the maximum number of characters required to decode a given number of bytes.
        public override int GetMaxCharCount(int byteCount)
        {
            return byteCount * 2;
        }

        // Converts a hexadecimal character to its corresponding integer value.
        private static int GetValue(char digit)
        {
            if (digit > 0x2F && digit < 0x3A)
                return digit - 48;
            if (digit > 0x40 && digit < 0x47)
                return digit - 55;
            if (digit > 0x60 && digit < 0x67)
                return digit - 87;
            throw BadBaseException(digit);
        }

        // Converts an integer value to its corresponding hexadecimal character.
        private static char GeDigit(int value)
        {
            return value >= 0xA ? (char)(value + 0x37) : (char)(value + 0x30);
        }

        // Creates another instance of the Base16Encoding.
        public override object Clone()
        {
            return new Base16Encoding();
        }
    }
}