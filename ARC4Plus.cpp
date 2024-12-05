#include <iostream>
#include <vector>
#include <stdexcept>
#include <cstdlib>
#include <cstring>
#include <random>
#include <iomanip>
#include <sstream>

/*
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
*/

class ARC4CryptoTransform {
public:
    ARC4CryptoTransform(const std::vector<uint8_t>& key, const std::vector<uint8_t>& iv);
    ~ARC4CryptoTransform();

    std::vector<uint8_t> transform_final_block(const std::vector<uint8_t>& input_buffer, int input_offset, int input_count);

private:
    void _initialize(const std::vector<uint8_t>& key, const std::vector<uint8_t>& iv);
    void _lcr(std::vector<uint8_t>& sblock, const std::vector<uint8_t>& iv);
    void _ksa(std::vector<uint8_t>& sblock, const std::vector<uint8_t>& key);
    void _prga(std::vector<uint8_t>& sblock);
    int transform_block(const std::vector<uint8_t>& input_buffer, int input_offset, int input_count, std::vector<uint8_t>& output_buffer, int output_offset);

    std::vector<uint8_t> _s1;
    std::vector<uint8_t> _s2;
    std::vector<uint8_t> _key;
    std::vector<uint8_t> _iv;
    int _x1, _y1, _x2, _y2;
    bool _disposed;

    static const std::vector<uint8_t> _A;
    static const std::vector<uint8_t> _C;
};

const std::vector<uint8_t> ARC4CryptoTransform::_A = {
    0x09, 0x0D, 0x11, 0x15, 0x19, 0x1d, 0x21, 0x25,
    0x29, 0x2d, 0x31, 0x35, 0x39, 0x3d, 0x41, 0x45,
    0x49, 0x4d, 0x51, 0x55, 0x59, 0x5d, 0x61, 0x65,
    0x69, 0x6d, 0x71, 0x75, 0x79, 0x7d, 0x81, 0x85,
    0x89, 0x8d, 0x91, 0x95, 0x99, 0x9d, 0xa1, 0xa5,
    0xa9, 0xad, 0xb1, 0xb5, 0xb9, 0xbd, 0xc1, 0xc5,
    0xc9, 0xcd, 0xd1, 0xd5, 0xd9, 0xdd, 0xe1, 0xe5,
    0xe9, 0xed, 0xf1, 0xf5, 0xf9
};

const std::vector<uint8_t> ARC4CryptoTransform::_C = {
    0x05, 0x07, 0x0B, 0x0D, 0x11, 0x13, 0x17, 0x1d,
    0x1f, 0x25, 0x29, 0x2b, 0x2f, 0x35, 0x3b, 0x3d,
    0x43, 0x47, 0x49, 0x4f, 0x53, 0x59, 0x61, 0x65,
    0x67, 0x6b, 0x6d, 0x71, 0x7f, 0x83, 0x89, 0x8b,
    0x95, 0x97, 0x9d, 0xa3, 0xa7, 0xad, 0xb3, 0xb5,
    0xbf, 0xc1, 0xc5, 0xc7, 0xd3, 0xdf, 0xe3, 0xe5,
    0xe9, 0xef, 0xf1, 0xfb
};

ARC4CryptoTransform::ARC4CryptoTransform(const std::vector<uint8_t>& key, const std::vector<uint8_t>& iv)
    : _s1(256, 0), _s2(256, 0), _key(key), _iv(iv), _x1(0), _y1(0), _x2(0), _y2(0), _disposed(false) {
    if (key.empty()) {
        throw std::invalid_argument("Key cannot be null.");
    }
    if (iv.size() != 4) {
        throw std::invalid_argument("Initialization vector must be 4 bytes long.");
    }
    _initialize(key, iv);
}

ARC4CryptoTransform::~ARC4CryptoTransform() {
    if (!_disposed) {
        _s1.clear();
        _s2.clear();
        _x1 = _y1 = _x2 = _y2 = 0;
        _disposed = true;
    }
}

void ARC4CryptoTransform::_initialize(const std::vector<uint8_t>& key, const std::vector<uint8_t>& iv) {
    _lcr(_s1, iv);

    std::vector<uint8_t> shifted_iv(4);
    for (size_t i = 0; i < 4; ++i) {
        shifted_iv[i] = (iv[i] + 128) & 0xFF;
    }
    std::rotate(shifted_iv.rbegin(), shifted_iv.rbegin() + 1, shifted_iv.rend());
    _lcr(_s2, shifted_iv);

    _ksa(_s1, key);
    _ksa(_s2, key);
}

void ARC4CryptoTransform::_lcr(std::vector<uint8_t>& sblock, const std::vector<uint8_t>& iv) {
    int r = iv[0];
    int x = iv[1];
    int a = _A[iv[2] % _A.size()];
    int c = _C[iv[3] % _C.size()];

    for (int i = 0; i < 256; ++i) {
        x = (a * x + c) & 0xFF;
        sblock[i] = r ^ x;
    }
}

void ARC4CryptoTransform::_ksa(std::vector<uint8_t>& sblock, const std::vector<uint8_t>& key) {
    int j = 0;
    for (int i = 0; i < 256; ++i) {
        j = (j + sblock[i] + key[i % key.size()]) % 256;
        std::swap(sblock[i], sblock[j]);
    }

    for (int i = 0; i < 256; ++i) {
        _prga(sblock);
    }
}

void ARC4CryptoTransform::_prga(std::vector<uint8_t>& sblock) {
    _x1 = (_x1 + 1) & 0xFF;
    _y1 = (_y1 + sblock[_x1]) & 0xFF;
    std::swap(sblock[_x1], sblock[_y1]);
}

int ARC4CryptoTransform::transform_block(const std::vector<uint8_t>& input_buffer, int input_offset, int input_count, std::vector<uint8_t>& output_buffer, int output_offset) {
    if (input_buffer.empty()) {
        throw std::invalid_argument("Input buffer cannot be null.");
    }
    if (output_buffer.empty()) {
        throw std::invalid_argument("Output buffer cannot be null.");
    }
    if (input_offset < 0 || input_offset >= input_buffer.size()) {
        throw std::out_of_range("Input offset is out of range for the input buffer.");
    }
    if (input_count < 0 || input_offset + input_count > input_buffer.size()) {
        throw std::out_of_range("Input count is out of range for the input buffer.");
    }
    if (output_offset < 0 || output_offset >= output_buffer.size()) {
        throw std::out_of_range("Output offset is out of range for the output buffer.");
    }
    if (output_offset + input_count > output_buffer.size()) {
        throw std::out_of_range("Output buffer is too small to receive the transformed data.");
    }

    for (int i = 0; i < input_count; ++i) {
        _prga(_s1);
        uint8_t k1 = _s1[(_s1[_x1] + _s1[_y1]) & 0xFF];
        _prga(_s2);
        uint8_t k2 = _s2[(_s2[_x2] + _s2[_y2]) & 0xFF];

        uint8_t k = (k1 + k2) ^ ((k1 << 5) | (k2 >> 3));
        k = k & 0xFF;

        output_buffer[output_offset + i] = input_buffer[input_offset + i] ^ k;
    }

    return input_count;
}

std::vector<uint8_t> ARC4CryptoTransform::transform_final_block(const std::vector<uint8_t>& input_buffer, int input_offset, int input_count) {
    if (input_buffer.empty()) {
        throw std::invalid_argument("Input buffer cannot be null.");
    }
    if (input_offset < 0 || input_offset >= input_buffer.size()) {
        throw std::out_of_range("Input offset is out of range for the input buffer.");
    }
    if (input_count < 0 || input_offset + input_count > input_buffer.size()) {
        throw std::out_of_range("Input count is out of range for the input buffer.");
    }

    std::vector<uint8_t> final_block(input_count);
    transform_block(input_buffer, input_offset, input_count, final_block, 0);
    return final_block;
}

// Test function
int main() {
    std::vector<uint8_t> key = { 'p', 'a', 's', 's', 'w', 'o', 'r', 'd' };
    std::vector<uint8_t> data = { 'H', 'e', 'l', 'l', 'o', ',', ' ', 'w', 'o', 'r', 'l', 'd', '!' };
    std::random_device rd;
    std::mt19937 gen(rd());
    std::uniform_int_distribution<> dis(0, std::numeric_limits<uint32_t>::max());
    uint32_t seed = dis(gen);

    for (int i = 1; i <= 5; ++i) {
        ARC4CryptoTransform rc4(key, { (seed ^ i) & 0xFF, (seed ^ i) >> 8 & 0xFF, (seed ^ i) >> 16 & 0xFF, (seed ^ i) >> 24 & 0xFF });
        std::vector<uint8_t> encrypted = rc4.transform_final_block(data, 0, data.size());
        ARC4CryptoTransform rc4_decrypt(key, { (seed ^ i) & 0xFF, (seed ^ i) >> 8 & 0xFF, (seed ^ i) >> 16 & 0xFF, (seed ^ i) >> 24 & 0xFF });
        std::vector<uint8_t> decrypted = rc4_decrypt.transform_final_block(encrypted, 0, encrypted.size());
        std::string text(decrypted.begin(), decrypted.end());
        std::cout << "Phase: " << i << std::endl;
        std::cout << "Seed: " << (seed ^ i) << std::endl;
        std::cout << "Encrypted text: " << base64_encode(encrypted.data(), encrypted.size()) << std::endl;
        std::cout << "Decrypted text: " << text << std::endl;
        std::cout << std::endl;
    }

    std::cout << "Press Enter to exit...";
    std::cin.get();
    return 0;
}

// Helper function to encode data in base64
std::string base64_encode(const uint8_t* data, size_t len) {
    static const char* base64_chars =
        "ABCDEFGHIJKLMNOPQRSTUVWXYZ"
        "abcdefghijklmnopqrstuvwxyz"
        "0123456789+/";

    std::string encoded;
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
                encoded += base64_chars[char_array_4[i]];
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
            encoded += base64_chars[char_array_4[j]];

        while ((i++ < 3))
            encoded += '=';
    }

    return encoded;
}
