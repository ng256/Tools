using System;
using System.Collections.Generic;
using System.Linq;

namespace TestApp
{
    internal class Program
    {
        static byte[] _bytes = GetBytes(new Random().Next());

        public static byte[] GetBytes(int seed)
        {
            // For GF(256) the modular polynomial x^8 + x^4 + x^3 + x + 1 (0x11B) is commonly used.
            int degree = 8;
            int modulus = 0x11B;
            Gf gf = new Gf(degree, modulus);

            List<byte> bytes = new List<byte>(256);
            for (int i = 0; i < 256; i++)
            {
                bytes.Add((byte)(i ^ 0x55));
            }

            // Initialize pseudorandom number generator based on seed and GF operations
            // Use simple LCG (linear congruential generator) with parameters depending on field
            int a = gf.Pow(gf.Generator, seed % (gf.FieldSize - 1));
            int c = seed;

            // Fisher-Yates shuffle using GF-based LCG
            for (int i = bytes.Count - 1; i > 0; i--)
            {
                // Use GF to generate index.
                int j = (gf.Pow(a, i) + c) % (i + 1);
                // Swap elements.
                byte temp = bytes[i];
                bytes[i] = bytes[j];
                bytes[j] = temp;
            }

            return bytes.ToArray();
        }
        private static unsafe bool Validate(byte[] bytes)
        {
            if (bytes == null || bytes.Length != 256)
            {
                return false;
            }

            fixed (byte* bytesPtr = bytes)
            {
                int* seenPtr = stackalloc[] { 0, 0, 0, 0, 0, 0, 0, 0 };

                for (int i = 0; i < 256; i++)
                {
                    byte currentByte = bytesPtr[i];
                    int mask = 1 << (currentByte & 0x1F);
                    int offset = currentByte >> 5;

                    if ((seenPtr[offset] & mask) != 0)
                        return false;

                    seenPtr[offset] |= mask;
                }
            }

            return true;
        }

        static void Main(string[] args)
        {
            Console.Write(string.Join(", ", _bytes.Select(b => $"{b:X2}")));
            
            bool result = Validate(_bytes);
        }
    }

    public class Gf
    {
        private int _degree;
        private int _modulus;
        private int[] _logTable;
        private int[] _expTable;
        public int Generator = 2;
        public readonly int FieldSize;

        public Gf(int degree, int modulus)
        {
            this._degree = degree;
            this._modulus = modulus;
            FieldSize = 1 << degree;
           
            _logTable = new int[this._modulus];
            _expTable = new int[this._modulus];

            _expTable[0] = 1;
            for (int i = 1; i < this._modulus; i++)
            {
                _expTable[i] = (_expTable[i - 1] * Generator) % _modulus;
                _logTable[_expTable[i]] = i;
            }
        }

        public int Log(int x)
        {
           
            return _logTable[x];
        }

        public int Pow(int x, int power)
        {
            
            int result = 1;
            while (power > 0)
            {
                if ((power & 1) == 1)
                    result = (result * x) % _modulus;
                x = (x * x) % _modulus;
                power >>= 1;
            }
            return result;
        }

        public int Mul(int a, int b)
        {
            
            if (a == 0 || b == 0)
                return 0;
            return _expTable[(_logTable[a] + _logTable[b]) % (_modulus - 1)];
        }

        public int Add(int a, int b)
        {
            
            return (a ^ b);
        }
    }

}
