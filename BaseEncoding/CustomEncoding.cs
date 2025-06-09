/***************************************************************************************

• File: CustomEncoding.cs

• Description:

    Provides the abstract base class for custom binary-to-text encoding implementations 
    using configurable alphabets. Defines core functionality including alphabet 
    validation, leading zero handling, and common conversion infrastructure.

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

using System.Globalization;

namespace System.Text
{
    // CustomEncoding is an abstract class that serves as a base for custom encoding implementations.
    internal abstract class CustomEncoding : BaseEncoding
    {
        // Stores the encoding alphabet characters.
        private readonly char[] _encodingTable;

        // Lookup table for character-to-value mapping.
        private readonly int[] _decodingTable;

        // Base of the encoding system (alphabet size).
        private readonly int _base;

        // Property to get the base of the encoding system.
        protected int Base => _base;

        // Returns a zero digit for the current alphabet.
        protected char ZeroDigit => GetDigit(0);

        // Checks for unprintable characters in an array, which are not suitable for encoding.
        private static bool HasUnprintableChars(char[] chars)
        {
            if (chars == null || chars.Length == 0)
                return false;

            foreach (char c in chars)
            {
                // Nul character.
                if (c == 0)
                    return true;

                // Process ASCII range.
                if (c <= 0x7F)
                {
                    // Control characters or whitespace (0-32, 127).
                    if (c <= 0x20 || c == 0x7F)
                        return true;
                }

                // Process Unicode range.
                else
                {
                    // C1 control characters (128-159).
                    if (c <= 0x9F)
                        return true;

                    // Unicode whitespace.
                    if (char.IsWhiteSpace(c))
                        return true;

                    // Problematic Unicode categories.
                    const UnicodeCategory UnassignedCategory = (UnicodeCategory)29;
                    UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(c);
                    if (category is UnicodeCategory.Control or
                        UnicodeCategory.Format or
                        UnicodeCategory.Surrogate or
                        UnicodeCategory.PrivateUse or
                        UnassignedCategory)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        // Checks for duplicate characters in an array, which would make the encoding ambiguous.
        private static bool HasDuplicateChars(char[] chars)
        {
            // Handle small arrays.
            if (chars == null || chars.Length <= 1)
                return false;

            if (chars.Length <= 16)
            {
                for (int i = 0; i < chars.Length; i++)
                {
                    for (int j = i + 1; j < chars.Length; j++)
                    {
                        if (chars[i] == chars[j])
                            return true;
                    }
                }
                return false;
            }

            // More characters than possible distinct values.
            if (chars.Length > 0x10000)
                return true;

            // Bit masks for fast ASCII duplicate checking.
            ulong lowBits = 0;   // Characters 0-63.
            ulong highBits = 0;  // Characters 64-127.
            HashSet<char>? nonAsciiSet = null;  // Lazy initialization for non-ASCII.

            foreach (char c in chars)
            {
                // ASCII character processing.
                if (c < 0x80)
                {
                    if (c < 0x40)
                    {
                        ulong mask = 1UL << c;
                        if ((lowBits & mask) != 0) return true;  // Duplicate found.
                        lowBits |= mask;
                    }
                    else
                    {
                        ulong mask = 1UL << (c - 0x40);
                        if ((highBits & mask) != 0) return true;  // Duplicate found.
                        highBits |= mask;
                    }
                }
                // Non-ASCII character processing.
                else
                {
                    nonAsciiSet ??= new HashSet<char>(chars.Length);
                    if (!nonAsciiSet.Add(c)) return true;  // Duplicate found.
                }
            }

            return false;
        }

        // Default constructor.
        private CustomEncoding() : base(0) { }

        // Primary constructor that takes a character array as the alphabet.
        // Initializes the encoding with a specific alphabet, validating it and setting up encoding and decoding tables
        protected CustomEncoding(char[] alphabet) : this()
        {
            // Validate alphabet input.
            if (alphabet == null)
                throw new ArgumentNullException("Alphabet cannot be null");
            if (alphabet == null || alphabet.Length < 2)
                throw new ArgumentException("Alphabet must have at least 2 characters");
            if (alphabet.Length > 0x10000)
                throw new ArgumentException("Alphabet size exceeds Unicode character limit");


            // Reject unprintable characters.
            if (HasUnprintableChars(alphabet))
                throw new ArgumentException("Alphabet contains unprintable characters");

            // Reject duplicate characters.
            if (HasDuplicateChars(alphabet))
                throw new ArgumentException("Alphabet contains duplicate characters");

            // Create a defensive copy of the alphabet.
            int length = alphabet.Length;
            _base = length;
            _encodingTable = new char[length];
            Array.Copy(alphabet, _encodingTable, length);

            // Build optimized decoding table.
            if (alphabet.Length > 0)
            {
                // Find maximum character for table sizing.
                char maxChar = alphabet.Max();
                _decodingTable = new int[maxChar + 1];
                Array.Fill(_decodingTable, -1);  // Initialize with invalid marker.

                // Populate decoding table.
                for (int i = 0; i < length; i++)
                {
                    char c = alphabet[i];
                    // Verify character uniqueness within alphabet.
                    if (_decodingTable[c] != -1)
                        throw new ArgumentException($"Duplicate character '{c}' in alphabet");
                    _decodingTable[c] = i;  // Map character to its position.
                }
            }
            else
            {
                _decodingTable = Array.Empty<int>();
            }
        }

        // String-based constructor provides a convenient way to initialize the encoding using a strin.
        protected CustomEncoding(string alphabet)
            : this(alphabet?.ToCharArray() ?? throw new ArgumentNullException(nameof(alphabet)))
        {
        }

        // Copy constructor initializes a new instance of CustomEncoding by copying an existing instance.
        protected CustomEncoding(CustomEncoding source) : base(0)
        {
            _encodingTable = source._encodingTable;
            _decodingTable = source._decodingTable;
            _base = source._base;
        }

        // Convert a value to its corresponding character in the encoding alphabet.
        protected char GetDigit(int value)
        {
            return _encodingTable[value];
        }

        // Convert character from the encoding alphabet to its numeric value.
        protected int GetValue(char digit)
        {
            // Check character bounds.
            if (digit >= _decodingTable.Length || digit < 0)
                throw BadBaseException(digit);

            int value = _decodingTable[digit];
            // Check if character is in alphabet.
            if (value < 0)
                throw BadBaseException(digit);

            return value;
        }

        // Counts leading zero characters and returns remaining characters.
        protected (int leadingZeros, char[] remaining) CountLeadingZeros(char[] chars, int index, int count)
        {
            int leadingZeros = 0;
            // Count consecutive leading zero characters.
            for (int i = index; i < index + count; i++)
            {
                if (chars[i] != _encodingTable[0]) break;
                leadingZeros++;
            }

            int remainingCount = count - leadingZeros;
            if (remainingCount == 0)
                return (leadingZeros, Array.Empty<char>());

            // Extract non-zero characters.
            char[] remaining = new char[remainingCount];
            Array.Copy(chars, index + leadingZeros, remaining, 0, remainingCount);
            return (leadingZeros, remaining);
        }

        // Counts leading zero bytes and returns remaining bytes.
        protected (int leadingZeros, byte[] remaining) CountLeadingZeros(byte[] bytes, int index, int count)
        {
            int leadingZeros = 0;
            // Count consecutive leading zero bytes.
            for (int i = index; i < index + count; i++)
            {
                if (bytes[i] != 0) break;
                leadingZeros++;
            }

            int remainingCount = count - leadingZeros;
            if (remainingCount == 0)
                return (leadingZeros, Array.Empty<byte>());

            // Extract non-zero bytes.
            byte[] remaining = new byte[remainingCount];
            Array.Copy(bytes, index + leadingZeros, remaining, 0, remainingCount);
            return (leadingZeros, remaining);
        }

        // Writes leading zero characters to buffer.
        protected int WriteLeadingZeroChars(char[] chars, int index, int count)
        {
            for (int i = 0; i < count; i++)
                chars[index + i] = ZeroDigit;  // Write zero character.
            return count;
        }

        // Writes leading zero bytes to buffer.
        protected void WriteLeadingZeros(byte[] bytes, int index, int count)
        {
            for (int i = 0; i < count; i++)
                bytes[index + i] = 0;  // Write zero byte.
        }

        // Standard Encoding method implementations.

        // Estimates maximum byte count for character conversion.
        public override int GetMaxByteCount(int charCount)
        {
            return charCount + 1;
        }

        // Estimates maximum character count for byte conversion.
        public override int GetMaxCharCount(int byteCount)
        {
            return byteCount * 8 + 1;
        }

        // Gets the byte count for a string.
        public override int GetByteCount(string s)
        {
            return GetByteCount(s.ToCharArray(), 0, s.Length);
        }

        // Encodes a string into a byte array.
        public override int GetBytes(string s, int charIndex, int charCount, byte[] bytes, int byteIndex)
        {
            return GetBytes(s.ToCharArray(), charIndex, charCount, bytes, byteIndex);
        }

        // Decodes a byte array into a string.
        public override string GetString(byte[] bytes)
        {
            return GetString(bytes, 0, bytes.Length);
        }

        // Decodes a portion of a byte array into a string.
        public override string GetString(byte[] bytes, int index, int count)
        {
            char[] chars = new char[GetCharCount(bytes, index, count)];
            int length = GetChars(bytes, index, count, chars, 0);
            return new string(chars, 0, length);
        }
    }
}
