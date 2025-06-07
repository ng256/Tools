using System.Collections.Generic;

public static bool HasDuplicateChars(char[] chars)
{
    // Edge cases: 0 or 1 character
    if (chars.Length <= 1)
        return false;

    // Pigeonhole Principle: if there are more characters than unique char values
    if (chars.Length > 65536)
        return true;

    ulong lowBits = 0;   // For characters 0-63
    ulong highBits = 0;  // For characters 64-127
    HashSet<char>? nonAsciiSet = null;

    foreach (char c in chars)
    {
        // Handling ASCII (0-127)
        if (c < 128)
        {
            if (c < 64)
            {
                ulong mask = 1UL << c;
                if ((lowBits & mask) != 0)
                    return true;
                lowBits |= mask;
            }
            else
            {
                ulong mask = 1UL << (c - 64);
                if ((highBits & mask) != 0)
                    return true;
                highBits |= mask;
            }
        }
        // Handling non-ASCII (> 127)
        else
        {
            nonAsciiSet ??= new HashSet<char>();
            if (!nonAsciiSet.Add(c))
                return true;
        }
    }

    return false;
}
