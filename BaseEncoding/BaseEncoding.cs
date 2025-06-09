/***************************************************************************************

•  File: BaseEncoding.cs

• Description:

It  is  an  abstract  class  BaseEncoding,  which  is  a  descendant  of  the  Encoding
class for creating various encodings.

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
    /// <summary>
    /// Represents the base class for custom base encodings (e.g., Base32, Base64).
    /// Inherits from <see cref="Encoding"/> and provides factory methods and utility logic.
    /// </summary>
    public abstract class BaseEncoding : Encoding
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BaseEncoding"/> class with the specified code page.
        /// </summary>
        /// <param name="codePage">The code page identifier for the encoding.</param>
        protected BaseEncoding(int codePage) : base(codePage)
        {
        }

        /// <summary>
        /// Returns a <see cref="BaseEncoding"/> instance that corresponds to the provided custom alphabet.
        /// </summary>
        /// <param name="alphabet">A string representing the alphabet used for encoding.</param>
        /// <returns>
        /// An instance of <see cref="CustomEncoding"/>, depending on the alphabet length.
        /// </returns>
        public static new BaseEncoding GetEncoding(string alphabet)
        {
            ArgumentNullException.ThrowIfNull(alphabet);

            return alphabet.Length > CustomEncodingSmall.MAX_BASE
                ? new CustomEncodingBig(alphabet)
                : new CustomEncodingSmall(alphabet);
        }

        /// <summary>
        /// Returns a <see cref="BaseEncoding"/> instance corresponding to the specified <see cref="BaseEncodingStyle"/>.
        /// </summary>
        /// <param name="encoding">The desired base encoding style.</param>
        /// <returns>
        /// A concrete implementation of <see cref="BaseEncoding"/> such as <see cref="Base32Encoding"/>, <see cref="Base64Encoding"/>, etc.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the encoding style is not supported.</exception>
        public static BaseEncoding GetEncoding(BaseEncodingStyle encoding)
        {
            switch (encoding)
            {
                case BaseEncodingStyle.Binary:
                    return new Base2Encoding();
                case BaseEncodingStyle.Octal:
                    return new Base8Encoding();
                case BaseEncodingStyle.Hexadecimal:
                    return new Base16Encoding();
                case BaseEncodingStyle.Base32:
                    return new Base32Encoding();
                case BaseEncodingStyle.Base64:
                    return new Base64Encoding();
                default:
                    throw new ArgumentOutOfRangeException(nameof(encoding), encoding, null);
            }
        }

        /// <summary>
        /// When overridden in a derived class, encodes a set of characters from the specified character array into a sequence of bytes.
        /// </summary>
        /// <param name="chars">The character array containing the characters to encode.</param>
        /// <param name="charIndex">The index of the first character to encode.</param>
        /// <param name="charCount">The number of characters to encode.</param>
        /// <param name="bytes">The byte array to contain the resulting sequence of bytes.</param>
        /// <param name="byteIndex">The index at which to start writing the resulting bytes.</param>
        /// <returns>The actual number of bytes written to the output array.</returns>
        public override int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex)
        {
            ValidateInput(chars, charIndex, charCount);
            ValidateOutput(bytes, byteIndex);

            return 0; // This method should be overridden to implement actual encoding logic.
        }

        /// <summary>
        /// When overridden in a derived class, decodes a sequence of bytes from the specified byte array into a set of characters.
        /// </summary>
        /// <param name="bytes">The byte array containing the sequence of bytes to decode.</param>
        /// <param name="byteIndex">The index of the first byte to decode.</param>
        /// <param name="byteCount">The number of bytes to decode.</param>
        /// <param name="chars">The character array to contain the resulting characters.</param>
        /// <param name="charIndex">The index at which to start writing the resulting characters.</param>
        /// <returns>The actual number of characters written to the output array.</returns>
        public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex)
        {
            ValidateInput(bytes, byteIndex, byteCount);
            ValidateOutput(chars, charIndex);

            return 0; // This method should be overridden to implement actual decoding logic.
        }

        /// <summary>
        /// Calculates the number of bytes produced by encoding a set of characters from the specified character array.
        /// </summary>
        /// <param name="chars">The character array containing the characters to encode.</param>
        /// <param name="index">The index of the first character to encode.</param>
        /// <param name="count">The number of characters to encode.</param>
        /// <returns>The number of bytes required to encode the specified characters.</returns>
        public override int GetByteCount(char[] chars, int index, int count)
        {
            ValidateInput(chars, index, count);

            return GetMaxByteCount(GetMaxCount(chars, index, count));
        }

        /// <summary>
        /// Calculates the number of characters produced by decoding a sequence of bytes from the specified byte array.
        /// </summary>
        /// <param name="bytes">The byte array containing the bytes to decode.</param>
        /// <param name="index">The index of the first byte to decode.</param>
        /// <param name="count">The number of bytes to decode.</param>
        /// <returns>The number of characters required to decode the specified bytes.</returns>
        public override int GetCharCount(byte[] bytes, int index, int count)
        {
            ValidateInput(bytes, index, count);

            return GetMaxCharCount(GetMaxCount(bytes, index, count));
        }

        /// <summary>
        /// Calculates the number of characters that can be obtained by decoding the specified byte array segment.
        /// This method can be overridden by derived classes to customize decoding logic.
        /// </summary>
        /// <param name="chars">The character array into which the result will be decoded.</param>
        /// <param name="charIndex">The starting index in the character array.</param>
        /// <param name="charCount">The number of characters to use in the array.</param>
        /// <param name="bytes">The byte array containing the encoded data.</param>
        /// <param name="byteIndex">The starting index in the byte array.</param>
        /// <returns>The estimated number of characters resulting from decoding the byte data.</returns>
        protected virtual int GetCharCount(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex)
        {
            ValidateInput(chars, charIndex, charCount);
            ValidateOutput(bytes, byteIndex);

            int maxCharCount = GetMaxCharCount(GetMaxCount(bytes, byteIndex));
            return Math.Min(GetMaxCount(chars, charIndex, charCount), maxCharCount);
        }

        /// <summary>
        /// Calculates the number of bytes that can be obtained by encoding the specified character array segment.
        /// This method can be overridden by derived classes to customize encoding logic.
        /// </summary>
        /// <param name="bytes">The byte array into which the encoded data will be written.</param>
        /// <param name="byteIndex">The starting index in the byte array.</param>
        /// <param name="byteCount">The number of bytes to write.</param>
        /// <param name="chars">The character array containing the characters to encode.</param>
        /// <param name="charIndex">The starting index in the character array.</param>
        /// <returns>The estimated number of bytes resulting from encoding the character data.</returns>
        protected virtual int GetByteCount(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex)
        {
            ValidateInput(bytes, byteIndex, byteCount);
            ValidateOutput(chars, charIndex);

            int maxByteCount = GetMaxByteCount(GetMaxCount(chars, charIndex));
            byteCount = Math.Min(GetMaxCount(bytes, byteIndex, byteCount), maxByteCount);
            return byteCount;
        }

        // Validates the input array and index/count parameters. Throws appropriate exceptions if arguments are invalid.
        protected internal static void ValidateInput<T>(T[] input, int index, int count)
        {
            ArgumentNullException.ThrowIfNull(input);
            ArgumentOutOfRangeException.ThrowIfNegative(index);
            ArgumentOutOfRangeException.ThrowIfNegative(count);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(index + count, input.Length, "index + count");
        }

        // Validates the output buffer and index. Throws appropriate exceptions if arguments are invalid.
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

        // Returns an exception if decoding input data contains an invalid symbol.
        protected internal static Exception BadBaseException(char digit)
        {
            return new FormatException($"Invalid digit {digit} for the specified base.");
        }
    }
}
