private static int FindBestRectangle(int square)
{
    // Bitmask for checking primality of numbers 0-63
    const ulong primeMask = 0x22280088AA005604UL;

    // Check valid range and that the number is not prime
    if (square < 4 || square > 64 || (primeMask & (1UL << square)) != 0)
    {
        return -1;
    }

    // Compute ceil(sqrt(square)) using bit manipulation
    int limit = 0;
    int bit = 1 << 6;
    while (bit > 0)
    {
        limit |= bit;
        if (limit * limit > square)
            limit ^= bit;
        bit >>= 1;
    }
    if (limit * limit < square) limit++;

    // Find divisor that yields sides closest to a square
    for (int i = limit; i >= 2; i--)
    {
        if (square % i == 0)
        {
            int j = square / i;
            return Math.Max(i, j);   // larger side
        }
    }
    return -1;   // should not be reached if number is composite
}
