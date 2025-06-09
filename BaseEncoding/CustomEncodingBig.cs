/***************************************************************************************

•   File: CustomEncodingBig.cs

•   Description:

    Implements BigInteger-based encoding/decoding for arbitrary-sized data. Provides 
    high compatibility and precision for large inputs using configurable alphabets.

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

using System.Numerics;

namespace System.Text;

internal sealed class CustomEncodingBig : CustomEncoding
{
    // Primary constructor that takes a character array as the alphabet.
    public CustomEncodingBig(char[] alphabet) : base(alphabet)
    {
    }

    // Overloaded constructor that takes a string alphabet.
    public CustomEncodingBig(string alphabet)
        : base(alphabet?.ToCharArray() ?? throw new ArgumentNullException(nameof(alphabet)))
    {
    }

    // Calculates number of bytes needed to store decoded characters.
    public override int GetByteCount(char[] chars, int index, int count)
    {
        ValidateInput(chars, index, count);
        if (count == 0) return 0;

        // Count leading zeros and get remaining characters.
        var (leadingZeros, remainingChars) = CountLeadingZeros(chars, index, count);
        if (remainingChars.Length == 0) return leadingZeros;

        // Convert remaining characters to BigInteger.
        BigInteger value = DecodeCharsToBigInteger(remainingChars);
        // Get minimal big-endian byte representation.
        byte[] bytes = GetMinimalBigEndianBytes(value);
        return leadingZeros + bytes.Length;
    }

    // Converts characters to bytes and stores in buffer.
    public override int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex)
    {
        // Validate the input and output parameters.
        base.GetBytes(chars, charIndex, charCount, bytes, byteIndex);

        if (charCount == 0) return 0;

        // Count leading zeros and get remaining characters.
        var (leadingZeros, remainingChars) = CountLeadingZeros(chars, charIndex, charCount);
        byte[] decodedBytes = remainingChars.Length > 0
            ? GetMinimalBigEndianBytes(DecodeCharsToBigInteger(remainingChars))
            : Array.Empty<byte>();

        int totalBytes = leadingZeros + decodedBytes.Length;
        // Check output buffer capacity.
        if (bytes.Length - byteIndex < totalBytes)
            throw new ArgumentException("Output buffer too small");

        // Write leading zeros and decoded bytes to buffer.
        WriteLeadingZeros(bytes, byteIndex, leadingZeros);
        Array.Copy(decodedBytes, 0, bytes, byteIndex + leadingZeros, decodedBytes.Length);
        return totalBytes;
    }

    // Calculates number of characters needed to store encoded bytes.
    public override int GetCharCount(byte[] bytes, int index, int count)
    {
        ValidateInput(bytes, index, count);
        if (count == 0) return 0;

        // Count leading zeros and get remaining bytes.
        var (leadingZeros, remainingBytes) = CountLeadingZeros(bytes, index, count);
        if (remainingBytes.Length == 0) return leadingZeros;

        // Convert bytes to BigInteger.
        BigInteger value = DecodeBytesToBigInteger(remainingBytes);
        return leadingZeros + CountSymbols(value);
    }
    
    // Converts bytes to characters and stores in buffer.
    public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex)
    {
        // Validate the input and output parameters.
        base.GetChars(bytes, byteIndex, byteCount, chars, charIndex);

        if (byteCount == 0) return 0;

        // Count leading zeros and write them as characters.
        var (leadingZeros, remainingBytes) = CountLeadingZeros(bytes, byteIndex, byteCount);
        int charsUsed = WriteLeadingZeroChars(chars, charIndex, leadingZeros);

        // Process remaining bytes if any.
        if (remainingBytes.Length > 0)
        {
            BigInteger value = DecodeBytesToBigInteger(remainingBytes);
            int charsWritten = WriteBigIntegerAsChars(value, chars, charIndex + leadingZeros);
            charsUsed += charsWritten;
        }

        return charsUsed;
    }

    // Converts character array to BigInteger.
    private BigInteger DecodeCharsToBigInteger(char[] chars)
    {
        int _base = Base;
        BigInteger value = BigInteger.Zero;
        // Process each character in sequence.
        for (int i = 0; i < chars.Length; i++)
        {
            int digit = GetValue(chars[i]);
            value = value * _base + digit;  // Base expansion.
        }
        return value;
    }

    // Converts byte array to BigInteger (little-endian with sign byte)
    private BigInteger DecodeBytesToBigInteger(byte[] bytes)
    {
        // Prepare buffer: reverse bytes + add zero sign byte.
        byte[] buf = new byte[bytes.Length + 1];
        for (int i = 0; i < bytes.Length; i++)
            buf[i] = bytes[bytes.Length - 1 - i];  // Reverse to little-endian.
        buf[bytes.Length] = 0;  // Positive sign byte.
        return new BigInteger(buf);
    }

    // Converts BigInteger to minimal big-endian byte array.
    private byte[] GetMinimalBigEndianBytes(BigInteger value)
    {
        // Get little-endian byte array.
        byte[] bytesLE = value.ToByteArray();
        // Remove trailing zero byte if present (sign byte artifact).
        if (bytesLE.Length > 1 && bytesLE[^1] == 0)
            Array.Resize(ref bytesLE, bytesLE.Length - 1);

        // Convert to big-endian.
        byte[] bigEndian = new byte[bytesLE.Length];
        for (int i = 0; i < bytesLE.Length; i++)
            bigEndian[i] = bytesLE[bytesLE.Length - 1 - i];  // Reverse bytes.

        return bigEndian;
    }

    // Counts symbols needed to represent BigInteger in current base.
    private int CountSymbols(BigInteger value)
    {
        if (value.IsZero) return 1;  // Special case: zero.

        int _base = Base;
        int count = 0;
        BigInteger temp = value;
        // Repeated division to count digits.
        while (temp > 0)
        {
            temp = BigInteger.DivRem(temp, _base, out BigInteger _);
            count++;
        }
        return count;
    }

    // Writes BigInteger as characters to buffer.
    private int WriteBigIntegerAsChars(BigInteger value, char[] chars, int index)
    {
        // Handle zero as special case.
        if (value.IsZero)
        {
            chars[index] = ZeroDigit;
            return 1;
        }

        // Use stack to reverse digit order.
        Stack<char> stack = new Stack<char>();
        int _base = Base;
        BigInteger temp = value;
        while (temp > 0)
        {
            temp = BigInteger.DivRem(temp, _base, out BigInteger remainder);
            stack.Push(GetDigit((int)remainder));  // Map remainder to character.
        }

        int length = stack.Count;
        // Check buffer capacity.
        if (chars.Length - index < length)
            throw new ArgumentException("Output char buffer too small");

        // Write characters from stack to buffer.
        foreach (char c in stack)
            chars[index++] = c;

        return length;
    }
}