/******************************************************************

•   File: CustomEncodingSmall.cs

•   Description:

    Provides optimized stack-based encoding/decoding for small to
    medium-sized data using configurable alphabets (up to 256 chars).
    Uses manual base conversion without BigInteger for efficiency.


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

******************************************************************/

namespace System.Text;

internal sealed class CustomEncodingSmall : CustomEncoding
{
    internal const int MAX_BASE = 0x100;

    // Primary constructor with character array alphabet.
    public CustomEncodingSmall(char[] alphabet) 
        : base(alphabet)
    {
        if (alphabet.Length > MAX_BASE)
            throw new ArgumentException("Alphabet length must be less than 256");
    }

    // String-based constructor.
    public CustomEncodingSmall(string alphabet)
        : base(alphabet?.ToCharArray() ?? throw new ArgumentNullException(nameof(alphabet)))
    {
    }

    // Calculate byte count for decoded characters.
    public override int GetByteCount(char[] chars, int index, int count)
    {
        ValidateInput(chars, index, count);
        if (count == 0) return 0;

        // Process leading zeros
        var (leadingZeros, remainingChars) = CountLeadingZeros(chars, index, count);
        if (remainingChars.Length == 0) return leadingZeros;

        // Calculate byte count without full conversion.
        int _base = Base;
        return leadingZeros + CalculateByteOutputLength(remainingChars, _base);
    }

    // Convert characters to bytes.
    public override int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex)
    {
        // Validate the input and output parameters.
        base.GetBytes(chars, charIndex, charCount, bytes, byteIndex);

        if (charCount == 0) return 0;

        // Process leading zeros.
        var (leadingZeros, remainingChars) = CountLeadingZeros(chars, charIndex, charCount);

        // Allocate buffer for conversion.
        int maxBytes = CalculateMaxByteOutput(remainingChars.Length);
        int _base = Base;
        Span<byte> buffer = stackalloc byte[maxBytes];
        Span<byte> decodedBytes = remainingChars.Length > 0
            ? ConvertBase(remainingChars, _base, MAX_BASE, buffer)
            : Span<byte>.Empty;

        int totalBytes = leadingZeros + decodedBytes.Length;
        // Validate output buffer size.
        if (bytes.Length - byteIndex < totalBytes)
            throw new ArgumentException("Output buffer too small");

        // Write leading zeros and decoded data.
        WriteLeadingZeros(bytes, byteIndex, leadingZeros);
        decodedBytes.CopyTo(new Span<byte>(bytes, byteIndex + leadingZeros, decodedBytes.Length));
        return totalBytes;
    }

    // Calculate character count for encoded bytes.
    public override int GetCharCount(byte[] bytes, int index, int count)
    {
        ValidateInput(bytes, index, count);
        if (count == 0) return 0;

        // Process leading zeros.
        var (leadingZeros, remainingBytes) = CountLeadingZeros(bytes, index, count);
        if (remainingBytes.Length == 0) return leadingZeros;

        // Calculate character count without full conversion.
        int _base = Base;
        return leadingZeros + CalculateCharOutputLength(remainingBytes, _base);
    }

    // Convert bytes to characters.
    public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex)
    {
        // Validate the input and output parameters.
        base.GetChars(bytes, byteIndex, byteCount, chars, charIndex);

        if (byteCount == 0) return 0;

        // Process leading zeros.
        var (leadingZeros, remainingBytes) = CountLeadingZeros(bytes, byteIndex, byteCount);
        int charsUsed = WriteLeadingZeroChars(chars, charIndex, leadingZeros);

        // Convert remaining bytes if any.
        if (remainingBytes.Length > 0)
        {
            // Allocate conversion buffer.
            int maxChars = CalculateMaxCharOutput(remainingBytes.Length);
            int _base = Base;
            Span<char> buffer = stackalloc char[maxChars];
            Span<char> encodedChars = ConvertBase(remainingBytes, MAX_BASE, _base, buffer);

            // Validate output buffer capacity.
            if (chars.Length - (charIndex + leadingZeros) < encodedChars.Length)
                throw new ArgumentException("Output char buffer too small");

            // Copy converted characters.
            encodedChars.CopyTo(new Span<char>(chars, charIndex + leadingZeros, chars.Length - charIndex - leadingZeros));
            charsUsed += encodedChars.Length;
        }

        return charsUsed;
    }

    // Calculate maximum byte output size.
    private int CalculateMaxByteOutput(int charCount)
    {
        int _base = Base;
        return (int)Math.Ceiling(charCount * Math.Log(_base) / Math.Log(256)) + 1;
    }

    // Calculate maximum character output size.
    private int CalculateMaxCharOutput(int byteCount)
    {
        int _base = Base;
        return (int)Math.Ceiling(byteCount * 8 / Math.Log(_base, 2)) + 1;
    }

    // Convert character span to byte span (base: fromBase → toBase).
    private Span<byte> ConvertBase(ReadOnlySpan<char> input, int fromBase, int toBase, Span<byte> output)
    {
        // Convert characters to numeric digits.
        Span<byte> digits = stackalloc byte[input.Length];
        for (int i = 0; i < input.Length; i++)
        {
            digits[i] = (byte)GetValue(input[i]);
        }

        int outputPos = output.Length;
        int start = 0;
        int len = digits.Length;

        // Process digits until none left.
        while (len > 0)
        {
            int remainder = 0;
            // Perform long division.
            for (int i = start; i < start + len; i++)
            {
                int value = digits[i] + remainder * fromBase;
                digits[i] = (byte)(value / toBase);
                remainder = value % toBase;
            }
            // Store result byte.
            output[--outputPos] = (byte)remainder;

            // Skip leading zeros in next iteration.
            while (len > 0 && digits[start] == 0)
            {
                start++;
                len--;
            }
        }

        // Return meaningful portion of output.
        return output.Slice(outputPos);
    }

    // Convert byte span to character span (base: fromBase → toBase).
    private Span<char> ConvertBase(ReadOnlySpan<byte> input, int fromBase, int toBase, Span<char> output)
    {
        // Copy input digits to working buffer.
        Span<byte> digits = stackalloc byte[input.Length];
        input.CopyTo(digits);

        int outputPos = output.Length;
        int start = 0;
        int len = digits.Length;

        // Process digits until none left.
        while (len > 0)
        {
            int remainder = 0;
            // Perform long division.
            for (int i = start; i < start + len; i++)
            {
                int value = digits[i] + remainder * fromBase;
                digits[i] = (byte)(value / toBase);
                remainder = value % toBase;
            }
            // Map remainder to alphabet character.
            output[--outputPos] = GetDigit(remainder);

            // Skip leading zeros in next iteration.
            while (len > 0 && digits[start] == 0)
            {
                start++;
                len--;
            }
        }

        // Return meaningful portion of output.
        return output.Slice(outputPos);
    }

    // Calculate exact byte output length.
    private int CalculateByteOutputLength(ReadOnlySpan<char> input, int fromBase)
    {
        if (input.Length == 0) return 0;

        // Convert characters to digits.
        Span<byte> digits = stackalloc byte[input.Length];
        for (int i = 0; i < input.Length; i++)
        {
            digits[i] = (byte)GetValue(input[i]);
        }

        int count = 0;
        int start = 0;
        int len = digits.Length;

        // Simulate conversion to count output bytes.
        while (len > 0)
        {
            int remainder = 0;
            for (int i = start; i < start + len; i++)
            {
                int value = digits[i] + remainder * fromBase;
                digits[i] = (byte)(value / 0x100);
                remainder = value % 0x100;
            }
            count++;

            // Skip leading zeros.
            while (len > 0 && digits[start] == 0)
            {
                start++;
                len--;
            }
        }

        return count;
    }

    // Calculate exact character output length.
    private int CalculateCharOutputLength(ReadOnlySpan<byte> input, int toBase)
    {
        if (input.Length == 0) return 0;

        // Copy input bytes to digit buffer.
        Span<byte> digits = stackalloc byte[input.Length];
        input.CopyTo(digits);

        int count = 0;
        int start = 0;
        int len = digits.Length;

        // Simulate conversion to count output characters.
        while (len > 0)
        {
            int remainder = 0;
            for (int i = start; i < start + len; i++)
            {
                int value = digits[i] + remainder * 0x100;
                digits[i] = (byte)(value / toBase);
                remainder = value % toBase;
            }
            count++;

            // Skip leading zeros.
            while (len > 0 && digits[start] == 0)
            {
                start++;
                len--;
            }
        }

        return count;
    }
}