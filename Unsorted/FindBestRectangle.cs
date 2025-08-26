private static bool FindBestRectangle(int square, out int rows, out int columns)
{
    // Initialize output parameters
    rows = 0;
    columns = 0;

    // Bitmask for checking primality of numbers 0-63
    const ulong primeMask = 0x22280088AA005604UL;

    // Check range and primality of the number
    if (square < 4 || square > 64 || (primeMask & (1UL << square)) != 0)
    {
        return false;
    }

    // Calculate ceil(sqrt(square)) using bit manipulation
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

    // Find the optimal divisor
    for (int i = limit; i >= 2; i--)
    {
        if (square % i == 0)
        {
            // Ensure rows >= columns
            int j = square / i;
            rows = Math.Max(i, j);
            columns = Math.Min(i, j);
            break;
        }
    }
    return true;
}
