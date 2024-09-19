using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace TestApp
{
    internal class Program
    {
        static byte[] _bytes = CalculatePi();

        static unsafe byte[] GenerateRandomBytes()
        {
            const int length = 15;
            byte[] result = new byte[length];
            int used = 0;
            Random random = new Random();

            for (int i = 0; i < length; i++)
            {
                int num;
                do
                {
                    num = random.Next(length);
                } while ((used & (1 << num)) != 0);

                result[i] = (byte)num;
                used |= 1 << num;
            }

            return result;
        }


        private static unsafe byte Kappa(int x)
        {
            const byte cstt = 0xFC;
            byte* lambdaVectors = stackalloc byte[] { 0x13, 0x26, 0x24, 0x30 };

            byte result = 0;
            for (int j = 0; j < 4; j++)
            {
                if (((x >> j) & 1) == 1)
                {
                    result ^= (byte) lambdaVectors[j];
                }
            }
            return (byte)(result ^ cstt);
        }

        private static unsafe byte[] CalculatePi()
        {
            var F = new GF2n(8, 0b100011101); // Using irreducible polynomial X^8 + X^4 + X^3 + X^2 + 1

            //byte* s = stackalloc byte[] { 0, 12, 9, 8, 7, 4, 14, 6, 5, 10, 2, 11, 1, 3, 13 };
            byte[] s = GenerateRandomBytes();

            byte[] pi = new byte[256];
            pi[0] = Kappa(0);

            for (int x = 1; x < 256; x++)
            {
                int l = F.Log(x);
                int i = l % 17;
                int j = l / 17;

                byte y = i == 0 
                    ? Kappa(16 - j) 
                    : (byte)(Kappa(16 - i) ^ (byte)F.Pow(F.Pow(F.Gen(), 17), s[j]));
                pi[x] = y;
            }

            return pi;
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


            /*_bytes[85] = 255;
            _bytes[255] = 1;
            _bytes[1] = 85;*/

            Console.Write(string.Join(", ", _bytes));
            //Console.Write(string.Join(", ", _bytes.Select(b => $"{b:X}")));


            bool result = Validate(_bytes);
        }
    }

    public class GF2n
    {
        private readonly int _degree;
        private readonly int _modulus;
        private readonly int[] _logTable;
        private readonly int[] _expTable;

        public GF2n(int degree, int modulus)
        {
            _degree = degree;
            _modulus = modulus;
            int size = (1 << degree);
            _logTable = new int[size];
            _expTable = new int[size];

            int x = 1;
            for (int i = 0; i < size; i++)
            {
                _expTable[i] = x;
                _logTable[x] = i;
                x <<= 1;
                if ((x & size) != 0)
                {
                    x ^= _modulus;
                }
            }
            _expTable[size - 1] = 1;
        }

        public int Gen() => 2; // Generator (alpha) is x in GF(2^n)

        public int Log(int x) => _logTable[x];

        public int Pow(int x, int power)
        {
            if (x == 0) return 0;
            return _expTable[(_logTable[x] * power) % ((1 << _degree) - 1)];
        }
    }
}
