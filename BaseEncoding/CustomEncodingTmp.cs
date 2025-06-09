/********************************************************************

   •   File: CustomEncoding.cs

   •   Description.

       The CustomEncoding class provides custom binary-to-text
       encoding implementations using configurable alphabets.
       Supports efficient encoding/decoding of binary data
       to text representations.

******************************************************************/

using System.Globalization;

namespace System.Text
{
    internal sealed class CustomEncodingTmp : BaseEncoding
    {
        private const uint MAX_BITS_COUNT = 32;
        private readonly int _blockSize;
        private readonly int _blockCharsCount;
        private readonly char[] _encodingTable;
        private readonly int[] _decodingTable;
        private readonly ulong[] _powN;

        public CustomEncodingTmp(string alphabet) : base(0)
        {
            if (alphabet == null)
                throw new ArgumentNullException(nameof(alphabet));
            if (alphabet.Length == 0 || alphabet.Any(char.IsWhiteSpace))
                throw new ArgumentException("Alphabet must not be empty or contain whitespace.", nameof(alphabet));

            _encodingTable = alphabet.ToCharArray();

            if (!ValidatePrintableCharacters(_encodingTable))
                throw new ArgumentException("Alphabet must consist of printable characters only.", nameof(alphabet));

            if (HasDuplicateChars(_encodingTable))
                throw new ArgumentException("Alphabet must contain only unique characters.", nameof(alphabet));



            uint charsCount = (uint)alphabet.Length;
            uint x = charsCount;
            int bitsPerChar = 0;
            while ((x >>= 1) != 0)
                bitsPerChar++;
            int lcm = bitsPerChar;
            for (int i = 8, j = i; i != 0; lcm = j)
            {
                i = lcm % i;
            }

            _blockSize = (bitsPerChar / lcm) * 8;
            _blockCharsCount = _blockSize / bitsPerChar;

            _decodingTable = new int[_encodingTable.Max() + 1];

            for (int i = 0; i < _decodingTable.Length; i++)
            {
                _decodingTable[i] = -1;
            }

            for (int i = 0; i < charsCount; i++)
            {
                _decodingTable[_encodingTable[i]] = i;
            }

            int optimalBitsCount = 0;
            uint charsCountInBits = 0;

            int logBaseN = 0;
            for (uint i = charsCount; (i /= 2) != 0; logBaseN++) ;

            double charsCountLog = Math.Log(2, charsCount);
            double maxRatio = 0;

            for (int i = logBaseN; i <= MAX_BITS_COUNT; i++)
            {
                uint j = (uint)Math.Ceiling(i * charsCountLog);
                double ratio = (double)i / j;
                if (ratio > maxRatio)
                {
                    maxRatio = ratio;
                    optimalBitsCount = i;
                    charsCountInBits = j;
                }
            }

            _blockSize = optimalBitsCount;
            _blockCharsCount = (int)charsCountInBits;
            _powN = new ulong[_blockCharsCount];
            ulong pow = 1;
            for (int i = 0; i < _blockCharsCount - 1; i++)
            {
                _powN[_blockCharsCount - 1 - i] = pow;
                pow *= charsCount;
            }

            _powN[0] = pow;
        }

        public override int GetMaxByteCount(int charCount)
        {
            if (charCount < 0)
                throw new ArgumentOutOfRangeException(nameof(charCount));

            long numerator = (long)charCount * _blockSize + (long)_blockCharsCount * 8 - 1;
            int denominator = _blockCharsCount * 8;
            return (int)(numerator / denominator);
        }

        public override int GetMaxCharCount(int byteCount)
        {
            if (byteCount < 0)
                throw new ArgumentOutOfRangeException(nameof(byteCount));

            long numerator = (long)byteCount * 8 * _blockCharsCount + _blockSize - 1;
            return (int)(numerator / _blockSize);
        }

        public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex)
        {
            base.GetChars(bytes, byteIndex, byteCount, chars, charIndex);

            int totalBits = byteCount * 8;
            int fullBlocks = totalBits / _blockSize;
            int tailBits = totalBits % _blockSize;
            int tailChars = tailBits == 0 ? 0 : (tailBits * _blockCharsCount + _blockSize - 1) / _blockSize;
            int totalChars = fullBlocks * _blockCharsCount + tailChars;

            if (chars.Length - charIndex < totalChars)
                throw new ArgumentException("Insufficient space in the character array.");

            for (int i = 0; i < fullBlocks; i++)
            {
                int bitIndex = byteIndex * 8 + i * _blockSize;
                ulong value = ReadValue(bytes, bitIndex, _blockSize);
                EncodeBlock(chars, charIndex + i * _blockCharsCount, _blockCharsCount, value);
            }

            if (tailChars > 0)
            {
                int bitIndex = byteIndex * 8 + fullBlocks * _blockSize;
                ulong value = ReadValue(bytes, bitIndex, tailBits);
                EncodeBlock(chars, charIndex + fullBlocks * _blockCharsCount, tailChars, value);
            }

            return totalChars;
        }

        public override int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex)
        {
            base.GetBytes(chars, charIndex, charCount, bytes, byteIndex);

            int fullBlocks = charCount / _blockCharsCount;
            int tailChars = charCount % _blockCharsCount;
            int tailBits = tailChars == 0 ? 0 : (tailChars * _blockSize + _blockCharsCount - 1) / _blockCharsCount;
            int totalBits = fullBlocks * _blockSize + tailBits;
            int totalBytes = (totalBits + 7) / 8;

            if (bytes.Length - byteIndex < totalBytes)
                throw new ArgumentException("Insufficient space in the byte array.");

            for (int i = 0; i < fullBlocks; i++)
            {
                int currentCharIndex = charIndex + i * _blockCharsCount;
                ulong value = DecodeBlock(chars, currentCharIndex, _blockCharsCount);
                WriteValue(bytes, value, byteIndex * 8 + i * _blockSize, _blockSize);
            }

            if (tailChars > 0)
            {
                int currentCharIndex = charIndex + fullBlocks * _blockCharsCount;
                ulong value = DecodeBlock(chars, currentCharIndex, tailChars);
                if (tailBits < 64 && value >= (1UL << tailBits))
                    throw new FormatException("Overflow in tail block.");

                WriteValue(bytes, value, byteIndex * 8 + fullBlocks * _blockSize, tailBits);
            }

            return totalBytes;
        }

        public override int GetCharCount(byte[] bytes, int index, int count)
        {
            int bytesLength = bytes.Length;


            int mainBitsLength = bytes.Length * 8 / _blockSize * _blockSize;
            int tailBitsLength = bytes.Length * 8 - mainBitsLength;
            int mainCharsCount = mainBitsLength * _blockCharsCount / _blockSize;
            int tailCharsCount = (tailBitsLength * _blockCharsCount + _blockSize - 1) / _blockSize;
            int totalCharsCount = mainCharsCount + tailCharsCount;

            return totalCharsCount;
        }

        public override int GetByteCount(char[] chars, int index, int count)
        { 
            int totalBitsLength = ((chars.Length - 1) * _blockSize / _blockCharsCount + 8) / 8 * 8;
            int mainBitsLength = totalBitsLength / _blockSize * _blockSize;
            int tailBitsLength = totalBitsLength - mainBitsLength;
            int mainCharsCount = mainBitsLength * _blockCharsCount / _blockSize;
            int tailCharsCount = (tailBitsLength * _blockCharsCount + _blockSize - 1) / _blockSize;
            ulong tailBits = DecodeBlock(chars, mainCharsCount, tailCharsCount);
            if (tailBits >> tailBitsLength != 0)
            {
                totalBitsLength += 8;
                mainBitsLength = totalBitsLength / _blockSize * _blockSize;
                tailBitsLength = totalBitsLength - mainBitsLength;
                mainCharsCount = mainBitsLength * _blockCharsCount / _blockSize;
                tailCharsCount = (tailBitsLength * _blockCharsCount + _blockSize - 1) / _blockSize;
            }

            return totalBitsLength / 8;
        }

        private static ulong ReadValue(byte[] data, int bitIndex, int bitsCount)
        {
            ulong result = 0;

            int currentBytePos = Math.DivRem(bitIndex, 8, out int currentBitInBytePos);

            int xLength = Math.Min(bitsCount, 8 - currentBitInBytePos);
            if (xLength != 0)
            {
                result = ((ulong)data[currentBytePos] << 0x38 + currentBitInBytePos) >> 0x40 - xLength <<
                         bitsCount - xLength;

                currentBytePos += Math.DivRem(currentBitInBytePos + xLength, 8, out currentBitInBytePos);

                int x2Length = bitsCount - xLength;
                if (x2Length > 8)
                {
                    x2Length = 8;
                }

                while (x2Length > 0)
                {
                    xLength += x2Length;
                    result |= (ulong)data[currentBytePos] >> 8 - x2Length << bitsCount - xLength;

                    currentBytePos += Math.DivRem(currentBitInBytePos + x2Length, 8, out currentBitInBytePos);

                    x2Length = bitsCount - xLength;
                    if (x2Length > 8)
                    {
                        x2Length = 8;
                    }
                }
            }

            return result;
        }

        private static void WriteValue(byte[] data, ulong value, int bitIndex, int bitsCount)
        {
            unchecked
            {
                int currentBytePos = Math.DivRem(bitIndex, 8, out int currentBitInBytePos);

                int xLength = Math.Min(bitsCount, 8 - currentBitInBytePos);
                if (xLength != 0)
                {
                    byte x1 = (byte)(value << 0x40 - bitsCount >> 0x38 + currentBitInBytePos);
                    data[currentBytePos] |= x1;

                    currentBytePos += Math.DivRem(currentBitInBytePos + xLength, 8, out currentBitInBytePos);

                    int x2Length = bitsCount - xLength;
                    if (x2Length > 8)
                    {
                        x2Length = 8;
                    }

                    while (x2Length > 0)
                    {
                        xLength += x2Length;
                        byte x2 = (byte)(value >> bitsCount - xLength << 8 - x2Length);
                        data[currentBytePos] |= x2;

                        currentBytePos += Math.DivRem(currentBitInBytePos + x2Length, 8, out currentBitInBytePos);

                        x2Length = bitsCount - xLength;
                        if (x2Length > 8)
                        {
                            x2Length = 8;
                        }
                    }
                }
            }
        }

        private void EncodeBlock(char[] chars, int charIndex, int charCount, ulong block)
        {
            uint baseEncoding = (uint)_encodingTable.Length;
            int startCharIndex = charIndex;
            int endCharIndex = startCharIndex + charCount;
            while (charIndex < endCharIndex)
            {
                ulong blockCount = block / baseEncoding;
                ulong digit = block - blockCount * baseEncoding;
                block = blockCount;
                chars[charIndex++] = _encodingTable[(int)digit];
            }
        }

        

        private ulong DecodeBlock(char[] data, int charIndex, int charCount)
        {
            ulong result = 0;
            for (int i = 0; i < charCount; i++)
            {
                result += (ulong)_decodingTable[data[charIndex + i]] *
                          _powN[_blockCharsCount - 1 - i];
            }

            return result;
        }

        public override object Clone()
        {
            return new CustomEncodingTmp(new string(_encodingTable));
        }

        // Checks whether all characters in the array are printable
        private static bool ValidatePrintableCharacters(char[] chars)
        {
            foreach (char c in chars)
            {
                UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(c);

                // Reject all whitespace.
                if (char.IsWhiteSpace(c))
                {
                    return false;
                }

                // Reject non-printable categories.
                if (category == UnicodeCategory.Control ||
                    category == UnicodeCategory.Format ||
                    category == UnicodeCategory.Surrogate ||
                    category == UnicodeCategory.PrivateUse ||
                    category == UnicodeCategory.OtherNotAssigned ||
                    category == UnicodeCategory.LineSeparator ||
                    category == UnicodeCategory.ParagraphSeparator)
                {
                    return false;
                }
            }
            return true;
        }
        private static bool HasDuplicateChars(char[] chars)
        {
            if (chars.Length <= 64)
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

            var set = new HashSet<char>();
            foreach (char c in chars)
            {
                if (!set.Add(c))
                    return true;
            }
            return false;
        }
    }
}