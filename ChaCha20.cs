using System;
namespace System.Security.Cryptography;

/// <summary>
/// Implements the ChaCha20 custom stream cipher.
/// </summary>
public class ChaCha20 : ICryptoTransform
{
    private uint[] _state = new uint[16]; // State of the cipher
    private uint _blockCounter = 0; // Block counter for unique keystream generation

    // Constructor initializes the state with the key and IV
    public ChaCha20(byte[] key, byte[] iv)
    {
        InitializeState(key, iv);
    }

    /// <summary>
    /// Gets a value indicating whether multiple blocks can be transformed in one operation.
    /// </summary>
    public bool CanTransformMultipleBlocks => true;

    /// <summary>
    /// Gets the input block size in bytes.
    /// </summary>
    public int InputBlockSize => 64;

    /// <summary>
    /// Gets the output block size in bytes.
    /// </summary>
    public int OutputBlockSize => 64;

    /// <summary>
    /// Transforms a specified number of bytes from the input buffer to the output buffer.
    /// </summary>
    /// <param name="inputBuffer">The input data buffer.</param>
    /// <param name="inputOffset">The offset in the input buffer where the data starts.</param>
    /// <param name="outputBuffer">The output data buffer.</param>
    /// <param name="outputOffset">The offset in the output buffer where the data should be written.</param>
    /// <param name="count">The number of bytes to transform.</param>
    public void TransformBlock(byte[] inputBuffer, int inputOffset, byte[] outputBuffer, int outputOffset, int count)
    {
            // Buffer for the keystream
            byte[] keystream = new byte[64];
            int remainingBytes = count;

            // Process the input buffer block by block
            while (remainingBytes > 0)
            {
                GenerateBlock(keystream); // Generate the next keystream block
                int blockSize = Math.Min(64, remainingBytes); // Determine the size of the current block

                // XOR input data with keystream to produce encrypted/decrypted output
                for (int i = 0; i < blockSize; i++)
                {
                    outputBuffer[outputOffset + i] = (byte)(inputBuffer[inputOffset + i] ^ keystream[i]);
                }

                remainingBytes -= blockSize;
                inputOffset += blockSize;
                outputOffset += blockSize;
                _blockCounter++;
            }
    }

    /// <summary>
    /// Transforms the final block of data and returns the transformed bytes.
    /// </summary>
    /// <param name="inputBuffer">The input data buffer.</param>
    /// <param name="inputOffset">The offset in the input buffer where the data starts.</param>
    /// <param name="inputCount">The number of bytes to transform.</param>
    /// <returns>The transformed output data.</returns>
    public byte[] TransformFinalBlock(byte[] inputBuffer, int inputOffset, int inputCount)
    {
        byte[] outputBuffer = new byte[inputCount];
        TransformBlock(inputBuffer, inputOffset, outputBuffer, 0, inputCount);
        return outputBuffer;
    }

    // Static method to perform a left rotation on a 32-bit value
    private static uint RotateLeft(uint value, int count)
    {
        return (value << count) | (value >> (32 - count));
    }

    // Static method to perform a quarter round on four state elements
    private static void QuarterRound(ref uint a, ref uint b, ref uint c, ref uint d)
    {
        a += b; d = RotateLeft(d ^ a, 16);
        c += d; b = RotateLeft(b ^ c, 12);
        a += b; d = RotateLeft(d ^ a, 8);
        c += d; b = RotateLeft(b ^ c, 7);
    }

    // Static method to perform Galois Field multiplication (GF(2^8))
    private static uint GFMul(byte a, byte b)
    {
        uint result = 0;
        while (a > 0)
        {
            if ((a & 1) != 0)
            {
                result ^= b;
            }
            b = (byte)(b << 1);
            if ((b & 0x100) != 0)
            {
                b ^= 0x11D;  // Irreducible polynomial for GF(2^8)
            }
            a >>= 1;
        }
        return result;
    }

    // Static method to compute the inverse element in GF(2^8)
    private static byte GFInv(byte a)
    {
        byte result = a;
        for (int i = 0; i < 5; i++)  // Approximate number of iterations to find the inverse in GF(2^8)
        {
            result = GFMul(result, result);
        }
        return result;
    }

    // Initialize the state with the key and IV
    private void InitializeState(byte[] key, byte[] iv)
    {
        // Fill _state with transformations based on IV using GFMul for odd indices and GFInv for even indices
        for (int i = 0; i < 16; i++)  // We have 16 elements in _state
        {
            uint result = 1;

            // Iterate over all IV bytes and apply GFMul or GFInv depending on the index
            for (int j = 0; j < iv.Length; j++)
            {
                byte ivByte = iv[j];
                
                // For even indices of _state, use GFInv; for odd indices, use GFMul
                if (i % 2 == 0)  // Even index
                {
                    result = GFInv((byte)(ivByte ^ (i + j) & 0xFF));  // Apply GFInv transformation
                }
                else  // Odd index
                {
                    result = GFMul(result, ivByte);  // Apply GFMul transformation
                }
            }

            // Store the result in the current position of the state
            _state[i] = result;
        }

        // Use KSA (Key Scheduling Algorithm) to further mix the state with the key
        int i = 0;
        int j = 0;
        int keyIndex = 0;
        uint temp;

        // 20 rounds of mixing the state based on the key
        for (int round = 0; round < 20; round++)  // 20 rounds of mixing
        {
            i = (i + 1) % 16;
            j = (j + _state[i]) % 16;

            // Swap _state[i] and _state[j]
            temp = _state[i];
            _state[i] = _state[j];
            _state[j] = temp;

            // Mixing step: mix the state with key
            uint sum = _state[i] + _state[j];
            _state[i] = (_state[i] ^ (_state[j] + sum + key[keyIndex % key.Length])) % uint.MaxValue;

            keyIndex++;  // Move to the next byte of the key
        }
    }

    // Generate one block of keystream
    private void GenerateBlock(byte[] output)
    {
        uint[] workingState = (uint[])_state.Clone();
        workingState[12] ^= _blockCounter;

        for (int i = 0; i < 20; i += 2)
        {
            // Perform 10 rounds of quarter rounds
            QuarterRound(ref workingState[0], ref workingState[4], ref workingState[8], ref workingState[12]);
            QuarterRound(ref workingState[1], ref workingState[5], ref workingState[9], ref workingState[13]);
            QuarterRound(ref workingState[2], ref workingState[6], ref workingState[10], ref workingState[14]);
            QuarterRound(ref workingState[3], ref workingState[7], ref workingState[11], ref workingState[15]);

            // Perform another 10 rounds
            QuarterRound(ref workingState[0], ref workingState[5], ref workingState[10], ref workingState[15]);
            QuarterRound(ref workingState[1], ref workingState[6], ref workingState[11], ref workingState[12]);
            QuarterRound(ref workingState[2], ref workingState[7], ref workingState[8], ref workingState[13]);
            QuarterRound(ref workingState[3], ref workingState[4], ref workingState[9], ref workingState[14]);
        }

        // Convert the state to bytes and write to output buffer
        for (int i = 0; i < 16; i++)
        {
            workingState[i] += _state[i]; // Add the original state to the working state
            byte[] temp = BitConverter.GetBytes(workingState[i]);
            Array.Copy(temp, 0, output, i * 4, 4);
        }
    }

    /// <summary>
    /// Clears sensitive data from memory (obfuscates the state array to prevent leaks).
    /// </summary>
    public void Dispose()
    {
        Array.Clear(_state, 0, _state.Length);
        _blockCounter = 0;
    }
}
