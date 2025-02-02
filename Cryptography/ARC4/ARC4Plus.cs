/********************************************************************
 Implements a modified version of the RC4 encryption algorithm, 
 incorporating enhancements from RC4A and RC4+.
 This implementation differs from the standard RC4 algorithm 
 by using two S-blocks (S₁ and S₂) instead of one,
 and includes additional nonlinear transformations 
 for improved security and performance.

 RC4A (2004):
 - Introduced by Souradyuti Paul and Bart Preneel.
 - Uses two S-blocks (S₁ and S₂) with corresponding counters (j₁ and j₂).
 - Generates two bytes of ciphertext per iteration.
 - Algorithm:
   - i = 0
   - j₁ = 0
   - j₂ = 0
   - while generating:
     - i = i + 1
     - j₁ = (j₁ + S₁[i]) mod 256
     - Swap S₁[i] and S₁[j₁]
     - I₂ = (S₁[i] + S₁[j₁]) mod 256
     - output = S₂[I₂]
     - j₂ = (j₂ + S₂[i]) mod 256
     - Swap S₂[i] and S₂[j₂]
     - I₁ = (S₂[i] + S₂[j₂]) mod 256
     - output = S₁[I₁]
   - endwhile

 RC4+ (2008):
 - Introduced by Subhamoy Maitra and Goutam Paul.
 - Modifies the Key Scheduling Algorithm (KSA+) using 3-level scrambling.
 - Modifies the Pseudo-Random Generation Algorithm (PRGA+).
 - Algorithm:
   - All arithmetic operations are performed mod 256.
   - while generating:
     - i = i + 1
     - a = S[i]
     - j = j + a
     - b = S[j]
     - S[i] = b (swap S[i] and S[j])
     - S[j] = a
     - c = S[i << 5 ⊕ j >> 3] + S[j << 5 ⊕ i >> 3]
     - output = (S[a + b] + S[c ⊕ 0xAA]) ⊕ S[j + b]
   - endwhile

 ARC4(2022)
 Custom S-block Initialization:
 - The S-block is initialized with a pseudo-random byte array obtained using 
   the Linear Congruent Method (LCR) before being passed to PRGA.
 - This differs from the classical algorithm, where the S-block was initialized 
   with a sequence from 0 to 255 (S[i] = i).
 - For classic behavior, use ARC4SBlock.DefaultSBlock as an initialization vector.
 - Ensure the initialization vector is consistent to prevent corruption of decrypted data.

 LCR Details:
 - The LCR method calculates a sequence of random numbers X[i] using:
   - X[i+1] = (A • X[i] + C) MOD M
   - M is the modulus (a natural number M ≥ 2).
   - A is the factor (0 ≤ A < M).
   - C is the increment (0 ≤ C < M).
   - X[0] is the initial value (0 ≤ X[0] < M).
   - Index i changes sequentially within 0 ≤ i < M.
 - LCR creates a sequence of M non-duplicate pseudo-random values when:
   - The numbers C and M are coprime.
   - B = A - 1 is a multiple of P for every prime P that divides M.
   - B is a multiple of 4 if M is a multiple of 4.
 - Optimization:
   - X[i+1] = R ⊕ (A • X[i] + C) mod M
   - X[i] ∈ (0, 256)
   - X[0] is a random start value.
   - M = 256
   - R ∈ (0, 256) is a random constant for best randomization.
   - A ∈ (9, 249) and A - 1 can be divided by 4.
   - C ∈ (5, 251) and C is a prime number.
 ********************************************************************/
using System.Text;

namespace System.Security.Cryptography
{
    /// <summary>
    /// Implements a modified version of the RC4 encryption algorithm.
    /// </summary>
    public sealed unsafe class ARC4CryptoTransform : ICryptoTransform
    {
        // Precomputed values for the linear congruential transformation.
        private static readonly byte[] _A = // All LCR multiplier values.
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

        private static readonly byte[] _C = // All LCR increment values.
        {
            0x05, 0x07, 0x0B, 0x0D, 0x11, 0x13, 0x17, 0x1d,
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

        private byte[] _key;
        private byte[] _iv;

        // Indices for both state arrays.
        private int _x1, _y1, _x2, _y2;
        private bool _disposed = false;

        /// <summary>
        /// Size of the input data block in bytes.
        /// </summary>
        public int InputBlockSize => 1;

        /// <summary>
        /// Size of the output data block in bytes.
        /// </summary>
        public int OutputBlockSize => 1;

        /// <summary>
        /// Indicates whether multiple data blocks can be converted.
        /// </summary>
        public bool CanTransformMultipleBlocks => true;

        /// <summary>
        /// Indicates whether the transformation can be reused.
        /// </summary>
        public bool CanReuseTransform => true;

        /// <summary>
        /// Initializes the RC4 instance with a key and IV.
        /// </summary>
        /// <param name="key">The encryption key.</param>
        /// <param name="iv">The initialization vector (IV).</param>
        public ARC4CryptoTransform(byte[] key, byte[] iv)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key), "Key cannot be null.");
            if (iv == null)
                throw new ArgumentNullException(nameof(iv), "Initialization vector cannot be null.");
            if (iv.Length != 4)
                throw new ArgumentException("Initialization vector must be 4 bytes long.");

            byte* ivPtr = stackalloc byte[iv.Length];
            for (int i = 0; i < iv.Length; i++)
            {
                ivPtr[i] = iv[i];
            }

            // Perform Linear Congruential Random (LCR) generating and Key Scheduling Algorithm (KSA) on both state arrays.
            fixed (byte* s1Ptr = _s1, s2Ptr = _s2, keyPtr = key)
            {
                /***** Apply the LCR operation. *****/
                LCR(s1Ptr, ivPtr);

                // Shift the IV for the second state array.
                for (int i = 0; i < 4; i++)
                    ivPtr[i] = (byte)((ivPtr[i] + 128) & 0xFF);
                
                // Rotate the IV for further modification.
                byte swap = ivPtr[0];
                for (int i = 0; i < 3; i++) 
                    ivPtr[i] = ivPtr[i + 1];
                ivPtr[3] = swap;
                LCR(s2Ptr, ivPtr);

                /***** Apply the KSA operation. *****/
                int keyLength = key.Length;
                KSA(s1Ptr, keyPtr, keyLength, ref _x1, ref _y1);
                KSA(s2Ptr, keyPtr, keyLength, ref _x2, ref _y2);
            }

            // Initialize indices for the state arrays.
            _x1 = _y1 = _x2 = _y2 = 0;
        }

        /// <summary>
        /// Initializes the RC4 instance with a key and a 4-byte IV represented by an integer.
        /// </summary>
        /// <param name="key">The encryption key.</param>
        /// <param name="seed">The seed value is used as the initialization vector (IV).</param>
        public ARC4CryptoTransform(byte[] key, int seed)
            : this(key, BitConverter.GetBytes(seed))
        {
        }

        /// <summary>
        /// Initializes the RC4 instance with a key and a 4-byte IV represented by an integer.
        /// </summary>
        /// <param name="key">The encryption key.</param>
        /// <param name="seed">The seed value is used as the initialization vector (IV).</param>
        public ARC4CryptoTransform(byte[] key, uint seed)
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
            if (inputBuffer == null)
                throw new ArgumentNullException(nameof(inputBuffer), "Input buffer cannot be null.");
            if (outputBuffer == null)
                throw new ArgumentNullException(nameof(outputBuffer), "Output buffer cannot be null.");
            if (inputOffset < 0 || inputOffset >= inputBuffer.Length)
                throw new ArgumentOutOfRangeException(nameof(inputOffset), "Input offset is out of range for the input buffer.");
            if (inputCount < 0 || inputOffset + inputCount > inputBuffer.Length)
                throw new ArgumentOutOfRangeException(nameof(inputCount), "Input count is out of range for the input buffer.");
            if (outputOffset < 0 || outputOffset >= outputBuffer.Length)
                throw new ArgumentOutOfRangeException(nameof(outputOffset), "Output offset is out of range for the output buffer.");
            if (outputOffset + inputCount > outputBuffer.Length)
                throw new ArgumentOutOfRangeException(nameof(outputOffset), "Output buffer is too small to receive the transformed data.");


            fixed (byte* s1Ptr = _s1, s2Ptr = _s2)
            {
                for (int i = 0; i < inputCount; i++)
                {
                    // Generate an intermediate key bytes using PRGA operation.
                    PRGA(s1Ptr, ref _x1, ref _y1);
                    byte k1 = s1Ptr[(s1Ptr[_x1] + s1Ptr[_y1]) & 0xFF];
                    PRGA(s2Ptr, ref _x2, ref _y2);
                    byte k2 = s2Ptr[(s2Ptr[_x2] + s2Ptr[_y2]) & 0xFF];

                    // Combine the two key bytes using additional nonlinear transformations.
                    byte k = (byte)((k1 + k2) ^ ((k1 << 5) | (k2 >> 3)));

                    // XOR the key byte with the input to produce the output.
                    outputBuffer[outputOffset + i] = (byte)(inputBuffer[inputOffset + i] ^ k);
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
            if (inputBuffer == null)
                throw new ArgumentNullException(nameof(inputBuffer), "Input buffer cannot be null.");
            if (inputOffset < 0 || inputOffset >= inputBuffer.Length)
                throw new ArgumentOutOfRangeException(nameof(inputOffset), "Input offset is out of range for the input buffer.");
            if (inputCount < 0 || inputOffset + inputCount > inputBuffer.Length)
                throw new ArgumentOutOfRangeException(nameof(inputCount), "Input count is out of range for the input buffer.");

            byte[] finalBlock = new byte[inputCount];
            TransformBlock(inputBuffer, inputOffset, inputCount, finalBlock, 0);
            return finalBlock;
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

        // The Linear Congruential Generator (LCR) operation used in RC4.
        private static void LCR(byte* sblock, byte* iv)
        {
            // Extract the initialization vector (IV) values.
            int r = iv[0]; // Nolinear transformation value.
            int x = iv[1]; // First value.
            int a = _A[iv[2] % _A.Length]; // Multiplier.
            int c = _C[iv[3] % _C.Length]; // Increment.

            // Apply the Linear Congruential Transformation.
            for (int i = 0; i < 256; i++)
            {
                sblock[i] = (byte)(r ^ (x = (a * x + c) & 0xFF));
            }
        }

        // Performs PRGA operation.
        private static void PRGA(byte* sblock, ref int x, ref int y)
        {
            // Perform the swapping of state array values.
            x = (x + 1) & 0xFF;
            y = (y + sblock[x]) & 0xFF;
            Swap(sblock, x, y);
        }

        // The Key Scheduling Algorithm (KSA) used in RC4 to initialize the state array.
        private static void KSA(byte* sblock, byte* key, int keyLength, ref int x, ref int y)
        {
            if (keyLength < 1) 
                return;

            for (int i = 0, j = 0; i < 256; i++)
            {
                j = (j + sblock[i] + key[i % keyLength]) % 256;
                Swap(sblock, i, j);
            }

            // Skip the first 256 bytes to reduce correlation with the key.
            for (int i = 0; i < 256; i++)
            {
                // Performs PRGA operation.
                x = (x + 1) & 0xFF;
                y = (y + sblock[x]) & 0xFF;
                Swap(sblock, x, y);
            }
        }

        /// <summary>
        /// Releases the resources used by the <see cref="ARC4CryptoTransform"/> instance.
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                Array.Clear(_s1, 0, _s1.Length);
                Array.Clear(_s2, 0, _s2.Length);
                _x1 = _y1 = _x2 = _y2 = 0;
                _disposed = true;
            }
        }

        // Test function can be just removed.
        internal static void Main(string[] args)
        {
            Encoding encoding = Encoding.UTF8;
            Random random = new Random();
            byte[] key = encoding.GetBytes("password");
            byte[] data = encoding.GetBytes("Hello, world!");
            byte[] encrypted, decrypted;
            int seed = random.Next();
            for (int i = 1; i <= 5; i++)
            {

                using (var rc4 = new ARC4CryptoTransform(key, seed ^ i))
                {
                    encrypted = rc4.TransformFinalBlock(data, 0, data.Length);
                }

                using (var rc4 = new ARC4CryptoTransform(key, seed ^ i))
                {
                    decrypted = rc4.TransformFinalBlock(encrypted, 0, encrypted.Length);
                }
                string text = encoding.GetString(decrypted);
                Console.WriteLine($"Phase: {i}");
                Console.WriteLine($"Seed: {seed ^ i}");
                Console.WriteLine($"Encrypted text: {Convert.ToBase64String(encrypted)}");
                Console.WriteLine($"Decrypted text: {text}");
                Console.WriteLine();
            }
            Console.ReadKey(true);
        }
    }
}
/********************************************************************
Console output:

Phase: 1
Seed: 1095514486
Encrypted text: gtdaynDeKCjgKEUmug==
Decrypted text: Hello, world!

Phase: 2
Seed: 1095514485
Encrypted text: an9CzAucHsOu0cUq8g==
Decrypted text: Hello, world!

Phase: 3
Seed: 1095514484
Encrypted text: 489jTrsx2bcqQkQlnw==
Decrypted text: Hello, world!

Phase: 4
Seed: 1095514483
Encrypted text: oJcRi9HJyyS9mnnBYg==
Decrypted text: Hello, world!

Phase: 5
Seed: 1095514482
Encrypted text: lKWAu8vQNCcdL69tUA==
Decrypted text: Hello, world!
*********************************************************************/
