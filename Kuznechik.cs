using System;

public class Kuznechik
{
    private const int BLOCK_SIZE = 16; // Length of the block (16 bytes = 128 bits)
    private const int ROUNDS = 10;

    // Direct S-Box
    private static readonly byte[] Pi = new byte[256] {
        0xFC, 0xEE, 0xDD, 0x11, 0xCF, 0x6E, 0x31, 0x16,
        0xFB, 0xC4, 0xFA, 0xDA, 0x23, 0xC5, 0x04, 0x4D,
        0xC7, 0xB7, 0xEA, 0x1C, 0xA9, 0xA0, 0x9A, 0x74,
        0x10, 0x9E, 0x8B, 0x87, 0x59, 0x79, 0xA6, 0xB2,
        0xF6, 0x72, 0x3E, 0x3D, 0x5D, 0x99, 0xD5, 0x39,
        0x45, 0xB1, 0xE2, 0xC1, 0xC3, 0xE4, 0x2A, 0x9D,
        0xB0, 0xD3, 0xF7, 0xB4, 0x22, 0xBF, 0x1A, 0xA1,
        0x5E, 0x68, 0x82, 0x3C, 0x7A, 0x2D, 0x77, 0xC9,
        0xC8, 0x88, 0x0D, 0x64, 0xEA, 0xA3, 0xF8, 0x5C,
        0xCE, 0xA4, 0x78, 0x0B, 0xC2, 0xD4, 0x9F, 0xF4,
        0x83, 0x6A, 0x1E, 0xC6, 0x7E, 0xAB, 0x71, 0xF5,
        0x61, 0x2B, 0x3F, 0x15, 0x01, 0x6F, 0xBA, 0xE1,
        0x66, 0xE3, 0x0C, 0xEC, 0x2C, 0x5B, 0xF2, 0x14,
        0x8E, 0x9C, 0x9B, 0x7D, 0x67, 0x2F, 0x49, 0x4B,
        0xB8, 0x48, 0xC0, 0x4A, 0xD0, 0xE7, 0x1F, 0xF3,
        0xBD, 0x3B, 0x12, 0x8A, 0x1D, 0x24, 0x94, 0xBA,
        0x50, 0xE6, 0x7F, 0x5A, 0xAA, 0xED, 0xDA, 0xE5,
        0x80, 0x25, 0x46, 0xB9, 0xE8, 0x8C, 0x2E, 0x84,
        0x9E, 0xAA, 0xD9, 0xF0, 0xB5, 0xF1, 0xD1, 0x76
    };

    // Inverse S-Box
    private static readonly byte[] reverse_Pi = new byte[256] {
        0xA5, 0x2D, 0x32, 0x8F, 0x0E, 0x30, 0x38, 0xC0,
        0x09, 0x14, 0xC6, 0x4A, 0x20, 0xE3, 0x58, 0x62,
        0xA0, 0x4C, 0x1F, 0x8B, 0x7D, 0xD5, 0x46, 0xC1,
        0x3A, 0x99, 0x24, 0x8D, 0xB7, 0xED, 0xE2, 0x69,
        0x39, 0x6F, 0x1E, 0x6A, 0xA3, 0x5E, 0xB4, 0xB0,
        0x77, 0x15, 0x53, 0x73, 0x19, 0x0A, 0x65, 0x67,
        0x36, 0x2B, 0x57, 0x1B, 0xFD, 0xB3, 0xD2, 0x13,
        0x75, 0xE4, 0x5F, 0x8E, 0x8A, 0xD8, 0x63, 0xAD,
        0x43, 0x9A, 0xC4, 0x11, 0x4D, 0x8C, 0x7E, 0xB9,
        0x5A, 0xF5, 0xD7, 0xD3, 0x91, 0xC2, 0x10, 0xA1,
        0xF9, 0x26, 0x81, 0x2C, 0x72, 0x7F, 0xC3, 0x6B,
        0x6D, 0xE0, 0xE7, 0xE1, 0xA9, 0xB8, 0xED, 0xD0,
        0x48, 0xEB, 0xA4, 0xE9, 0xFF, 0x35, 0x4F, 0x52,
        0xC5, 0x38, 0x64, 0x7C, 0xF6, 0x01, 0x55, 0x3F,
        0x37, 0x02, 0x01, 0x1D, 0xF0, 0xCC, 0xA8, 0xEB,
        0x76, 0x41, 0x3C, 0xEE, 0xC9, 0xE8, 0x44, 0xC8,
        0x59, 0x6C, 0xA7, 0xB6, 0xDC, 0xCC, 0xE6, 0xA6,
        0x25, 0x47, 0xFB, 0xEB, 0xBA, 0xA2, 0xB1, 0x5D
    };

    // Linear transformation vector
    private static readonly byte[] l_vec = new byte[] {
        1, 148, 32, 133, 16, 194, 192, 1,
        251, 1, 192, 194, 16, 133, 32, 148
    };

    private byte[][] iter_C = new byte[32][];
    private byte[][] iter_key = new byte[ROUNDS + 1][];

    // Constructor
    public Kuznechik(byte[] key)
    {
        if (key.Length != 32) throw new ArgumentException("Key must be 256 bits (32 bytes).");
        ExpandKey(key);
    }

    private void ExpandKey(byte[] key)
    {
        // Key expansion logic
        iter_key[0] = key;

        for (int i = 1; i <= ROUNDS; i++)
        {
            iter_key[i] = new byte[BLOCK_SIZE];
            iter_key[i][0] = (byte)((iter_key[i - 1][0] + i) % 256);

            for (int j = 1; j < BLOCK_SIZE; j++)
            {
                iter_key[i][j] = iter_key[i - 1][j];
            }

            iter_key[i] = GOST_Kuz_L(iter_key[i]);
        }
    }

    private byte[] GOST_Kuz_X(byte[] a, byte[] b)
    {
        byte[] c = new byte[BLOCK_SIZE];
        for (int i = 0; i < BLOCK_SIZE; i++)
        {
            c[i] = (byte)(a[i] ^ b[i]);
        }
        return c;
    }

    private byte[] GOST_Kuz_S(byte[] in_data)
    {
        byte[] out_data = new byte[in_data.Length];
        for (int i = 0; i < BLOCK_SIZE; i++)
        {
            out_data[i] = Pi[in_data[i]];
        }
        return out_data;
    }

    private byte GOST_Kuz_GF_mul(byte a, byte b)
    {
        byte c = 0;
        byte hi_bit;
        for (int i = 0; i < 8; i++)
        {
            if ((b & 1) == 1)
                c ^= a;
            hi_bit = (byte)(a & 0x80);
            a <<= 1;
            if (hi_bit != 0)
                a ^= 0xC3; // Polynomial x^8 + x^7 + x^6 + x + 1
            b >>= 1;
        }
        return c;
    }

    private byte[] GOST_Kuz_R(byte[] state)
    {
        byte a_15 = 0;
        byte[] internal = new byte[BLOCK_SIZE];
        for (int i = 15; i >= 0; i--)
        {
            if (i == 0)
                internal[15] = state[i];
            else
                internal[i - 1] = state[i];
            a_15 ^= GOST_Kuz_GF_mul(state[i], l_vec[i]);
        }
        internal[15] = a_15;
        return internal;
    }

    private byte[] GOST_Kuz_L(byte[] in_data)
    {
        byte[] out_data = new byte[in_data.Length];
        byte[] internal = in_data;
        for (int i = 0; i < 16; i++)
        {
            internal = GOST_Kuz_R(internal);
        }
        return internal;
    }

    private byte[] GOST_Kuz_reverse_S(byte[] in_data)
    {
        byte[] out_data = new byte[in_data.Length];
        for (int i = 0; i < BLOCK_SIZE; i++)
        {
            out_data[i] = reverse_Pi[in_data[i]];
        }
        return out_data;
    }

    public byte[] Encrypt(byte[] blk)
    {
        if (blk.Length != BLOCK_SIZE) throw new ArgumentException("Block must be 128 bits (16 bytes).");
        byte[] state = blk;

        state = GOST_Kuz_X(state, iter_key[0]);
        for (int i = 1; i <= ROUNDS; i++)
        {
            state = GOST_Kuz_S(state);
            state = GOST_Kuz_L(state);
            state = GOST_Kuz_X(state, iter_key[i]);
        }
        return state;
    }

    public byte[] Decrypt(byte[] blk)
    {
        if (blk.Length != BLOCK_SIZE) throw new ArgumentException("Block must be 128 bits (16 bytes).");
        byte[] state = blk;

        state = GOST_Kuz_X(state, iter_key[ROUNDS]);
        for (int i = ROUNDS - 1; i >= 0; i--)
        {
            state = GOST_Kuz_reverse_S(state);
            state = GOST_Kuz_L(state);
            state = GOST_Kuz_X(state, iter_key[i]);
        }
        return state;
    }
}