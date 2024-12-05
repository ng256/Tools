"""
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
"""

import random
import struct

class ARC4CryptoTransform:
    # Precomputed values for the linear congruential transformation.
    _A = [
        0x09, 0x0D, 0x11, 0x15, 0x19, 0x1d, 0x21, 0x25,
        0x29, 0x2d, 0x31, 0x35, 0x39, 0x3d, 0x41, 0x45,
        0x49, 0x4d, 0x51, 0x55, 0x59, 0x5d, 0x61, 0x65,
        0x69, 0x6d, 0x71, 0x75, 0x79, 0x7d, 0x81, 0x85,
        0x89, 0x8d, 0x91, 0x95, 0x99, 0x9d, 0xa1, 0xa5,
        0xa9, 0xad, 0xb1, 0xb5, 0xb9, 0xbd, 0xc1, 0xc5,
        0xc9, 0xcd, 0xd1, 0xd5, 0xd9, 0xdd, 0xe1, 0xe5,
        0xe9, 0xed, 0xf1, 0xf5, 0xf9
    ]

    _C = [
        0x05, 0x07, 0x0B, 0x0D, 0x11, 0x13, 0x17, 0x1d,
        0x1f, 0x25, 0x29, 0x2b, 0x2f, 0x35, 0x3b, 0x3d,
        0x43, 0x47, 0x49, 0x4f, 0x53, 0x59, 0x61, 0x65,
        0x67, 0x6b, 0x6d, 0x71, 0x7f, 0x83, 0x89, 0x8b,
        0x95, 0x97, 0x9d, 0xa3, 0xa7, 0xad, 0xb3, 0xb5,
        0xbf, 0xc1, 0xc5, 0xc7, 0xd3, 0xdf, 0xe3, 0xe5,
        0xe9, 0xef, 0xf1, 0xfb
    ]

    def __init__(self, key, iv):
        if key is None:
            raise ValueError("Key cannot be null.")
        if iv is None:
            raise ValueError("Initialization vector cannot be null.")
        if len(iv) != 4:
            raise ValueError("Initialization vector must be 4 bytes long.")

        self._s1 = [0] * 256
        self._s2 = [0] * 256
        self._key = key
        self._iv = iv
        self._x1 = self._y1 = self._x2 = self._y2 = 0
        self._disposed = False

        self._initialize(key, iv)

    def _initialize(self, key, iv):
        # Perform Linear Congruential Random (LCR) generating and Key Scheduling Algorithm (KSA) on both state arrays.
        self._lcr(self._s1, iv)

        # Shift the IV for the second state array.
        shifted_iv = [(iv[i] + 128) & 0xFF for i in range(4)]

        # Rotate the IV for further modification.
        shifted_iv = shifted_iv[1:] + [shifted_iv[0]]
        self._lcr(self._s2, shifted_iv)

        # Apply the KSA operation.
        self._ksa(self._s1, key)
        self._ksa(self._s2, key)

    def _lcr(self, sblock, iv):
        # Extract the initialization vector (IV) values.
        r = iv[0]  # Nonlinear transformation value.
        x = iv[1]  # First value.
        a = self._A[iv[2] % len(self._A)]  # Multiplier.
        c = self._C[iv[3] % len(self._C)]  # Increment.

        # Apply the Linear Congruential Transformation.
        for i in range(256):
            x = (a * x + c) & 0xFF
            sblock[i] = r ^ x

    def _ksa(self, sblock, key):
        key_length = len(key)
        j = 0
        for i in range(256):
            j = (j + sblock[i] + key[i % key_length]) % 256
            sblock[i], sblock[j] = sblock[j], sblock[i]

        # Skip the first 256 bytes to reduce correlation with the key.
        for i in range(256):
            self._prga(sblock)

    def _prga(self, sblock):
        self._x1 = (self._x1 + 1) & 0xFF
        self._y1 = (self._y1 + sblock[self._x1]) & 0xFF
        sblock[self._x1], sblock[self._y1] = sblock[self._y1], sblock[self._x1]

    def transform_block(self, input_buffer, input_offset, input_count, output_buffer, output_offset):
        if input_buffer is None:
            raise ValueError("Input buffer cannot be null.")
        if output_buffer is None:
            raise ValueError("Output buffer cannot be null.")
        if input_offset < 0 or input_offset >= len(input_buffer):
            raise ValueError("Input offset is out of range for the input buffer.")
        if input_count < 0 or input_offset + input_count > len(input_buffer):
            raise ValueError("Input count is out of range for the input buffer.")
        if output_offset < 0 or output_offset >= len(output_buffer):
            raise ValueError("Output offset is out of range for the output buffer.")
        if output_offset + input_count > len(output_buffer):
            raise ValueError("Output buffer is too small to receive the transformed data.")

        for i in range(input_count):
            # Generate an intermediate key bytes using PRGA operation.
            self._prga(self._s1)
            k1 = self._s1[(self._s1[self._x1] + self._s1[self._y1]) & 0xFF]
            self._prga(self._s2)
            k2 = self._s2[(self._s2[self._x2] + self._s2[self._y2]) & 0xFF]

            # Combine the two key bytes using additional nonlinear transformations.
            k = (k1 + k2) ^ ((k1 << 5) | (k2 >> 3))

            # XOR the key byte with the input to produce the output.
            output_buffer[output_offset + i] = input_buffer[input_offset + i] ^ k

        return input_count

    def transform_final_block(self, input_buffer, input_offset, input_count):
        if input_buffer is None:
            raise ValueError("Input buffer cannot be null.")
        if input_offset < 0 or input_offset >= len(input_buffer):
            raise ValueError("Input offset is out of range for the input buffer.")
        if input_count < 0 or input_offset + input_count > len(input_buffer):
            raise ValueError("Input count is out of range for the input buffer.")

        final_block = bytearray(input_count)
        self.transform_block(input_buffer, input_offset, input_count, final_block, 0)
        return final_block

    def dispose(self):
        if not self._disposed:
            self._s1 = [0] * 256
            self._s2 = [0] * 256
            self._x1 = self._y1 = self._x2 = self._y2 = 0
            self._disposed = True

# Test function
if __name__ == "__main__":
    import base64

    encoding = 'utf-8'
    key = "password".encode(encoding)
    data = "Hello, world!".encode(encoding)
    seed = random.randint(0, 2**32 - 1)

    for i in range(1, 6):
        rc4 = ARC4CryptoTransform(key, struct.pack('I', seed ^ i))
        encrypted = rc4.transform_final_block(data, 0, len(data))
        rc4 = ARC4CryptoTransform(key, struct.pack('I', seed ^ i))
        decrypted = rc4.transform_final_block(encrypted, 0, len(encrypted))
        text = decrypted.decode(encoding)
        print(f"Phase: {i}")
        print(f"Seed: {seed ^ i}")
        print(f"Encrypted text: {base64.b64encode(encrypted).decode(encoding)}")
        print(f"Decrypted text: {text}")
        print()
"""
Console output:

Phase: 1
Seed: 873609066
Encrypted text: ERZaDhBq2JWuCpMiJg==
Decrypted text: Hello, world!

Phase: 2
Seed: 873609065
Encrypted text: YZ+MfvS2UXNGqx3m/Q==
Decrypted text: Hello, world!

Phase: 3
Seed: 873609064
Encrypted text: lYZ/sqZEg27S+JdBiw==
Decrypted text: Hello, world!

Phase: 4
Seed: 873609071
Encrypted text: HBkNwJictZoECAkMZQ==
Decrypted text: Hello, world!

Phase: 5
Seed: 873609070
Encrypted text: 9shUrU3qqYOCIN7Yog==
Decrypted text: Hello, world!
"""
