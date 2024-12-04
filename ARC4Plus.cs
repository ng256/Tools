using System;

namespace System.Security.Cryptography
{
    /// <summary>
    /// RC4 encryption algorithm implementation.
    /// </summary>
    public unsafe class RC4 : ICryptoTransform
    {
        // Precomputed values for the linear congruential transformation (LCR)
        private static readonly byte[] _A = // Multipliers.
        {
            0x09, 0x0D, 0x11, 0x15, 0x19, 0x1d, 0x21, 0x25, 
            0x29, 0x2d, 0x31, 0x35, 0x39, 0x3d, 0x41, 0x45,
            0x49, 0x4d, 0x51, 0x55, 0x59, 0x5d, 0x61, 0x65, 
            0x69, 0x6d, 0x71, 0x75, 0x79, 0x7d, 0x81, 0x85,
            0x89, 0x8d, 0x91, 0x95, 0x99, 0x9d, 0xa1, 0xa5, 
            0xa9, 0xad, 0xb1, 0xb5, 0xb9, 0xbd, 0xc1, 0xc5,
            0xc9, 0xcd, 0xd1, 0xd5, 0xd9, 0xdd, 0xe1, 0xe5, 
            0xe9, 0xed, 0xf1, 0xf5, 0xf9
        };

        private static readonly byte[] _C = // Increments.
        {
            0x05, 0x07, 0x0B, 0xD, 0x11, 0x13, 0x17, 0x1d, 
            0x1f, 0x25, 0x29, 0x2b, 0x2f, 0x35, 0x3b, 0x3d,
            0x43, 0x47, 0x49, 0x4f, 0x53, 0x59, 0x61, 0x65, 
            0x67, 0x6b, 0x6d, 0x71, 0x7f, 0x83, 0x89, 0x8b,
            0x95, 0x97, 0x9d, 0xa3, 0xa7, 0xad, 0xb3, 0xb5, 
            0xbf, 0xc1, 0xc5, 0xc7, 0xd3, 0xdf, 0xe3, 0xe5,
            0xe9, 0xef, 0xf1, 0xfb
        };

        // Internal state arrays for RC4.
        private byte[] _s1 = new byte[256];
        private byte[] _s2 = new byte[256];

        // Indices for both state arrays.
        private int _i1, _j1, _i2, _j2;
        private bool _disposed = false;

        public int InputBlockSize => 1;
        public int OutputBlockSize => 1;
        public bool CanTransformMultipleBlocks => true;
        public bool CanReuseTransform => true;

        /// <summary>
        /// Initializes the RC4 instance with a key and IV.
        /// </summary>
        /// <param name="key">The encryption key.</param>
        /// <param name="iv">The initialization vector (IV).</param>
        public RC4(byte[] key, byte[] iv)
        {
            if (key == null || iv == null)
                throw new ArgumentNullException("Key or IV cannot be null.");

            byte* ivPtr = stackalloc byte[iv.Length];
            for (int i = 0; i < iv.Length; i++)
            {
                ivPtr[i] = iv[i];
            }


            // Perform Linear Congruential Random (LCR) generating and Key Scheduling Algorithm (KSA) on both state arrays.
            fixed (byte* s1Ptr = _s1, s2Ptr = _s2)
            {
                // Apply the LCR operation
                int ivLength = iv.Length;
                LCR(_s1, ivPtr, ivLength);       
                ShiftIV(ivPtr, ivLength); // Modify the IV for the second state array
                RotateIV(ivPtr, ivLength); // Rotate the IV for further modification
                LCR(_s2, ivPtr, ivLength);

                // Apply the KSA operation
                KSA(s1Ptr, key);
                KSA(s2Ptr, key);
            }

            // Initialize indices for the state arrays
            _i1 = _j1 = _i2 = _j2 = 0;
        }

        /// <summary>
        /// Initializes the RC4 instance with a key and a 4-byte IV represented by an integer.
        /// </summary>
        /// <param name="key">The encryption key.</param>
        /// <param name="seed">The seed value is used as the initialization vector (IV).</param>
        public RC4(byte[] key, int seed)
            : this(key, BitConverter.GetBytes(seed))
        {
        }

        /// <summary>
        /// Transforms a block of data using the RC4 algorithm.
        /// </summary>
        /// <param name="inputBuffer">Input data buffer.</param>
        /// <param name="inputOffset">Starting offset in the input buffer.</param>
        /// <param name="inputCount">Number of bytes to process.</param>
        /// <param name="outputBuffer">Output data buffer.</param>
        /// <param name="outputOffset">Starting offset in the output buffer.</param>
        /// <returns>The number of bytes processed.</returns>
        public int TransformBlock(byte[] inputBuffer, int inputOffset, int inputCount, byte[] outputBuffer, int outputOffset)
        {
            if (inputBuffer == null || outputBuffer == null)
                throw new ArgumentNullException("Input or output buffer cannot be null.");

            fixed (byte* s1Ptr = _s1, s2Ptr = _s2)
            {
                for (int i = 0; i < inputCount; i++)
                {
                    byte k1 = NextByte(s1Ptr, ref _i1, ref _j1); // Generate a key byte from the first state array
                    byte k2 = NextByte(s2Ptr, ref _i2, ref _j2); // Generate a key byte from the second state array
                    outputBuffer[outputOffset + i] = (byte)(inputBuffer[inputOffset + i] ^ k1 ^ k2); // XOR with input to get output
                }
            }

            return inputCount;
        }

        /// <summary>
        /// Transforms the final block of data after processing all input blocks.
        /// </summary>
        /// <param name="inputBuffer">Input data buffer.</param>
        /// <param name="inputOffset">Starting offset in the input buffer.</param>
        /// <param name="inputCount">Number of bytes to process.</param>
        /// <returns>The transformed final block of data.</returns>
        public byte[] TransformFinalBlock(byte[] inputBuffer, int inputOffset, int inputCount)
        {
            byte[] finalBlock = new byte[inputCount];
            TransformBlock(inputBuffer, inputOffset, inputCount, finalBlock, 0);
            return finalBlock;
        }

        // The Linear Congruential Generator (LCR) operation used in RC4.
        private static void LCR(byte* sblock, byte* iv, int length)
        {
            if (length < 4)
                throw new ArgumentException("IV must be at least 4 bytes long.");

            // Extract the initialization vector (IV) values.
            int r = iv[0]; // The first byte of the IV.
            int x = iv[1]; // The second byte of the IV.
            int a = _A[iv[2] % _A.Length]; // Using precomputed values for 'a' (multiplier).
            int c = _C[iv[3] % _C.Length]; // Using precomputed values for 'c' (increment).
            const int m = 256; // Modulus for the transformation.

            // Apply the Linear Congruential Transformation.
            for (int i = 0; i < m; i++)
            {
                r = (a * r + c) % m;
                sblock[i] = (byte)(r);
            }
        }

        // Skips the first N bytes in the state array due to their correlation with the key.
        private static void DropDown(byte* sblock, int n, ref int x, ref int y)
        {
            for (int i = 0; i < n; i++)
            {
                // Perform the swapping of state array values.
                x = (x + 1) & 0xFF;
                y = (y + sblock[x]) & 0xFF;
                Swap(sblock, x, y);
            }
        }

        // Shifts the IV used in the RC4 initialization.
        private static void ShiftIV(byte* iv, int length)
        {
            for (int i = 0; i < length; i++)
            {
                iv[i] = (byte)(iv[i] + i);
            }
        }

        // Rotates the IV for further modification.
        private static void RotateIV(byte* iv, int length)
        {
            byte temp = iv[0];
            for (int i = 0; i < length - 1; i++)
            {
                iv[i] = iv[i + 1];
            }
            iv[length - 1] = temp;
        }

        // Performs PRGA operation.
        private static byte NextByte(byte* sblock, ref int x, ref int y)
        {
            x = (x + 1) & 0xFF;
            y = (y + sblock[x]) & 0xFF;
            Swap(sblock, x, y);
            return sblock[(sblock[x] + sblock[y]) & 0xFF];
        }

        // Swap state array values.
        private static void Swap(byte* array, int x, int y)
        {
            if (x != y)
            {
                array[x] ^= array[y];
                array[y] ^= array[x];
                array[x] ^= array[y];
            }
        }

        // The Key Scheduling Algorithm (KSA) used in RC4 to initialize the state array.
        private static void KSA(byte* sblock, byte[] key, ref int x, ref int y)
        {
            for (int i = 0, int j = 0; i < 256; i++)
            {
                j = (j + sblock[i] + key[i % key.Length]) % 256;
                
                Swap(sblock, i, j);
            }

            DropDown(sblock, 256, ref x, ref y); // Skip the first 256 bytes to remove correlation with the key.
        }

        // Releases the resources used by the RC4 instance.
        public void Dispose()
        {
            if (!_disposed)
            {
                Array.Clear(_s1, 0, _s1.Length);
                Array.Clear(_s2, 0, _s2.Length);
                _i1 = _j1 = _i2 = _j2 = 0;
                _disposed = true;
            }
        }
    }
}
