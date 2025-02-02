#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <stdint.h>
#include <stdbool.h>
#include <time.h>

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

#define ARRAY_SIZE(arr) (sizeof(arr) / sizeof((arr)[0]))

typedef struct {
    uint8_t _s1[256];
    uint8_t _s2[256];
    uint8_t* _key;
    uint8_t* _iv;
    int _x1, _y1, _x2, _y2;
    bool _disposed;
} ARC4CryptoTransform;

static const uint8_t _A[] = {
    0x09, 0x0D, 0x11, 0x15, 0x19, 0x1d, 0x21, 0x25,
    0x29, 0x2d, 0x31, 0x35, 0x39, 0x3d, 0x41, 0x45,
    0x49, 0x4d, 0x51, 0x55, 0x59, 0x5d, 0x61, 0x65,
    0x69, 0x6d, 0x71, 0x75, 0x79, 0x7d, 0x81, 0x85,
    0x89, 0x8d, 0x91, 0x95, 0x99, 0x9d, 0xa1, 0xa5,
    0xa9, 0xad, 0xb1, 0xb5, 0xb9, 0xbd, 0xc1, 0xc5,
    0xc9, 0xcd, 0xd1, 0xd5, 0xd9, 0xdd, 0xe1, 0xe5,
    0xe9, 0xed, 0xf1, 0xf5, 0xf9
};

static const uint8_t _C[] = {
    0x05, 0x07, 0x0B, 0x0D, 0x11, 0x13, 0x17, 0x1d,
    0x1f, 0x25, 0x29, 0x2b, 0x2f, 0x35, 0x3b, 0x3d,
    0x43, 0x47, 0x49, 0x4f, 0x53, 0x59, 0x61, 0x65,
    0x67, 0x6b, 0x6d, 0x71, 0x7f, 0x83, 0x89, 0x8b,
    0x95, 0x97, 0x9d, 0xa3, 0xa7, 0xad, 0xb3, 0xb5,
    0xbf, 0xc1, 0xc5, 0xc7, 0xd3, 0xdf, 0xe3, 0xe5,
    0xe9, 0xef, 0xf1, 0xfb
};

// Initializes the RC4 instance with a key and IV.
void initialize(ARC4CryptoTransform* self, const uint8_t* key, size_t key_length, const uint8_t* iv, size_t iv_length) {
    if (key == NULL) {
        fprintf(stderr, "Key cannot be null.\n");
        exit(EXIT_FAILURE);
    }
    if (iv == NULL) {
        fprintf(stderr, "Initialization vector cannot be null.\n");
        exit(EXIT_FAILURE);
    }
    if (iv_length != 4) {
        fprintf(stderr, "Initialization vector must be 4 bytes long.\n");
        exit(EXIT_FAILURE);
    }

    self->_key = (uint8_t*)malloc(key_length);
    self->_iv = (uint8_t*)malloc(iv_length);
    memcpy(self->_key, key, key_length);
    memcpy(self->_iv, iv, iv_length);

    _initialize(self, key, key_length, iv, iv_length);
}

// Performs the initialization of the S-blocks using LCR and KSA.
void _initialize(ARC4CryptoTransform* self, const uint8_t* key, size_t key_length, const uint8_t* iv, size_t iv_length) {
    _lcr(self->_s1, iv, iv_length);

    uint8_t shifted_iv[4];
    for (size_t i = 0; i < 4; ++i) {
        shifted_iv[i] = (iv[i] + 128) & 0xFF;
    }
    uint8_t temp = shifted_iv[0];
    for (size_t i = 0; i < 3; ++i) {
        shifted_iv[i] = shifted_iv[i + 1];
    }
    shifted_iv[3] = temp;
    _lcr(self->_s2, shifted_iv, 4);

    _ksa(self->_s1, key, key_length, &(self->_x1), &(self->_y1));
    _ksa(self->_s2, key, key_length, &(self->_x2), &(self->_y2));
}

// Performs the Linear Congruential Random (LCR) operation.
void _lcr(uint8_t* sblock, const uint8_t* iv, size_t iv_length) {
    int r = iv[0];
    int x = iv[1];
    int a = _A[iv[2] % ARRAY_SIZE(_A)];
    int c = _C[iv[3] % ARRAY_SIZE(_C)];

    for (int i = 0; i < 256; ++i) {
        x = (a * x + c) & 0xFF;
        sblock[i] = r ^ x;
    }
}

// Performs the Key Scheduling Algorithm (KSA).
void _ksa(uint8_t* sblock, const uint8_t* key, size_t key_length, int* x, int* y) {
    int j = 0;
    for (int i = 0; i < 256; ++i) {
        j = (j + sblock[i] + key[i % key_length]) % 256;
        uint8_t temp = sblock[i];
        sblock[i] = sblock[j];
        sblock[j] = temp;
    }

    for (int i = 0; i < 256; ++i) {
        _prga(sblock, x, y);
    }
}

// Performs the Pseudo-Random Generation Algorithm (PRGA).
void _prga(uint8_t* sblock, int* x, int* y) {
    *x = (*x + 1) & 0xFF;
    *y = (*y + sblock[*x]) & 0xFF;
    uint8_t temp = sblock[*x];
    sblock[*x] = sblock[*y];
    sblock[*y] = temp;
}

// Transforms a block of data using the RC4 algorithm.
int transform_block(ARC4CryptoTransform* self, const uint8_t* input_buffer, int input_offset, int input_count, uint8_t* output_buffer, int output_offset) {
    if (input_buffer == NULL) {
        fprintf(stderr, "Input buffer cannot be null.\n");
        exit(EXIT_FAILURE);
    }
    if (output_buffer == NULL) {
        fprintf(stderr, "Output buffer cannot be null.\n");
        exit(EXIT_FAILURE);
    }
    if (input_offset < 0 || input_offset >= input_count) {
        fprintf(stderr, "Input offset is out of range for the input buffer.\n");
        exit(EXIT_FAILURE);
    }
    if (input_count < 0 || input_offset + input_count > input_count) {
        fprintf(stderr, "Input count is out of range for the input buffer.\n");
        exit(EXIT_FAILURE);
    }
    if (output_offset < 0 || output_offset >= input_count) {
        fprintf(stderr, "Output offset is out of range for the output buffer.\n");
        exit(EXIT_FAILURE);
    }
    if (output_offset + input_count > input_count) {
        fprintf(stderr, "Output buffer is too small to receive the transformed data.\n");
        exit(EXIT_FAILURE);
    }

    for (int i = 0; i < input_count; ++i) {
        _prga(self->_s1, &(self->_x1), &(self->_y1));
        uint8_t k1 = self->_s1[(self->_s1[self->_x1] + self->_s1[self->_y1]) & 0xFF];
        _prga(self->_s2, &(self->_x2), &(self->_y2));
        uint8_t k2 = self->_s2[(self->_s2[self->_x2] + self->_s2[self->_y2]) & 0xFF];

        uint8_t k = (k1 + k2) ^ ((k1 << 5) | (k2 >> 3));
        k = k & 0xFF;

        output_buffer[output_offset + i] = input_buffer[input_offset + i] ^ k;
    }

    return input_count;
}

// Transforms the final block of data after processing all input blocks.
uint8_t* transform_final_block(ARC4CryptoTransform* self, const uint8_t* input_buffer, int input_offset, int input_count) {
    if (input_buffer == NULL) {
        fprintf(stderr, "Input buffer cannot be null.\n");
        exit(EXIT_FAILURE);
    }
    if (input_offset < 0 || input_offset >= input_count) {
        fprintf(stderr, "Input offset is out of range for the input buffer.\n");
        exit(EXIT_FAILURE);
    }
    if (input_count < 0 || input_offset + input_count > input_count) {
        fprintf(stderr, "Input count is out of range for the input buffer.\n");
        exit(EXIT_FAILURE);
    }

    uint8_t* final_block = (uint8_t*)malloc(input_count);
    transform_block(self, input_buffer, input_offset, input_count, final_block, 0);
    return final_block;
}

// Releases the resources used by the ARC4CryptoTransform instance.
void dispose(ARC4CryptoTransform* self) {
    if (!self->_disposed) {
        memset(self->_s1, 0, sizeof(self->_s1));
        memset(self->_s2, 0, sizeof(self->_s2));
        self->_x1 = self->_y1 = self->_x2 = self->_y2 = 0;
        free(self->_key);
        free(self->_iv);
        self->_disposed = true;
    }
}

// Helper function to encode data in base64.
char* base64_encode(const uint8_t* data, size_t len) {
    static const char* base64_chars =
        "ABCDEFGHIJKLMNOPQRSTUVWXYZ"
        "abcdefghijklmnopqrstuvwxyz"
        "0123456789+/";

    size_t encoded_len = 4 * ((len + 2) / 3);
    char* encoded = (char*)malloc(encoded_len + 1);
    if (encoded == NULL) {
        return NULL;
    }

    int i = 0;
    int j = 0;
    uint8_t char_array_3[3];
    uint8_t char_array_4[4];

    while (len--) {
        char_array_3[i++] = *(data++);
        if (i == 3) {
            char_array_4[0] = (char_array_3[0] & 0xfc) >> 2;
            char_array_4[1] = ((char_array_3[0] & 0x03) << 4) + ((char_array_3[1] & 0xf0) >> 4);
            char_array_4[2] = ((char_array_3[1] & 0x0f) << 2) + ((char_array_3[2] & 0xc0) >> 6);
            char_array_4[3] = char_array_3[2] & 0x3f;

            for (i = 0; (i < 4); i++)
                encoded[j++] = base64_chars[char_array_4[i]];
            i = 0;
        }
    }

    if (i) {
        for (j = i; j < 3; j++)
            char_array_3[j] = '\0';

        char_array_4[0] = (char_array_3[0] & 0xfc) >> 2;
        char_array_4[1] = ((char_array_3[0] & 0x03) << 4) + ((char_array_3[1] & 0xf0) >> 4);
        char_array_4[2] = ((char_array_3[1] & 0x0f) << 2) + ((char_array_3[2] & 0xc0) >> 6);
        char_array_4[3] = char_array_3[2] & 0x3f;

        for (j = 0; (j < i + 1); j++)
            encoded[j++] = base64_chars[char_array_4[j]];

        while ((i++ < 3))
            encoded[j++] = '=';
    }

    encoded[j] = '\0';
    return encoded;
}

int main() {
    const char* encoding = "utf-8";
    uint8_t key[] = { 'p', 'a', 's', 's', 'w', 'o', 'r', 'd' };
    uint8_t data[] = { 'H', 'e', 'l', 'l', 'o', ',', ' ', 'w', 'o', 'r', 'l', 'd', '!' };
    uint8_t* encrypted;
    uint8_t* decrypted;
    uint32_t seed = time(NULL);

    for (int i = 1; i <= 5; ++i) {
        ARC4CryptoTransform rc4;
        uint8_t iv[4];
        iv[0] = (seed ^ i) & 0xFF;
        iv[1] = (seed ^ i) >> 8 & 0xFF;
        iv[2] = (seed ^ i) >> 16 & 0xFF;
        iv[3] = (seed ^ i) >> 24 & 0xFF;
      
        initialize(&rc4, key, sizeof(key), iv, sizeof(iv));
        encrypted = transform_final_block(&rc4, data, 0, sizeof(data));

        initialize(&rc4, key, sizeof(key), iv, sizeof(iv));
        decrypted = transform_final_block(&rc4, encrypted, 0, sizeof(data));

        char* encoded = base64_encode(encrypted, sizeof(data));
        printf("Phase: %d\n", i);
        printf("Seed: %u\n", seed ^ i);
        printf("Encrypted text: %s\n", encoded);
        printf("Decrypted text: %s\n", decrypted);
        printf("\n");

        free(encoded);
        free(encrypted);
        free(decrypted);
        dispose(&rc4);
    }

    printf("Press Enter to exit...");
    getchar();
    return 0;
}
