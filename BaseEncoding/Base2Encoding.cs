/***************************************************************************************

•   File: Base2Encoding.cs

•   Description.

    Base2Encoding is designed to work with binary data and implements methods for 
    encoding and decoding data.


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
    // Implements the binary encoding.
    internal sealed class Base2Encoding : BaseEncoding
    {
        // Returns the name of the encoding
        public override string EncodingName => "binary";

        // Initialize a new instance of the Base2Encoding.
        public Base2Encoding()
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

            int bitPosition = 0;
            byte currentByte = 0;

            while (charIndex < endCharIndex)
            {
                currentByte = (byte)((currentByte << 1) | GetValue(chars[charIndex++]));
                bitPosition++;

                // Write byte when we have 8 bits
                if (bitPosition == 8)
                {
                    bytes[byteIndex++] = currentByte;
                    bitPosition = 0;
                    currentByte = 0;
                }
            }

            // If there are remaining bits that don’t complete a byte, shift and add them as the last byte.
            if (bitPosition > 0)
            {
                bytes[byteIndex++] = (byte)(currentByte << (8 - bitPosition));
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

            // Convert each byte to 8 characters (bits).
            while (byteIndex < endByteIndex)
            {
                byte value = bytes[byteIndex++];

                // Decode each bit into a character (0 or 1).
                for (int i = 7; i >= 0; i--)
                {
                    chars[charIndex++] = ((value & (1 << i)) != 0) ? '1' : '0';
                }
            }

            return charIndex - startCharIndex;
        }

        // Returns the maximum number of bytes required to encode a given number of characters.
        public override int GetMaxByteCount(int charCount)
        {
            // Each 8 characters represent 1 byte.
            return (charCount + 7) / 8;
        }

        // Returns the maximum number of characters required to decode a given number of bytes.
        public override int GetMaxCharCount(int byteCount)
        {
            // Each byte will produce 8 characters.
            return byteCount * 8;
        }

        // Converts a binary character ('0' or '1') to its corresponding integer value (0 or 1).
        private static int GetValue(char digit)
        {
            return digit switch
            {
                '0' => 0,
                '1' => 1,
                _ => throw BadBaseException(digit)
            };
        }

        // Creates another instance of the Base2Encoding.
        public override object Clone()
        {
            return new Base2Encoding();
        }
    }
}
