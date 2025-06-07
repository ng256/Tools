using System;
using System.Globalization;

public static bool HasUnprintableChars(char[] chars)
{
    // Constant for the Unassigned/OtherNotAssigned category
    const UnicodeCategory UnassignedCategory = (UnicodeCategory)29;
  
    if (chars == null || chars.Length == 0)
        return false;

    foreach (char c in chars)
    {
        // Handling ASCII characters (0-127)
        if (c <= 127)
        {
            // Check for control characters (0-31, 127) and whitespace (including space 32)
            if (c <= 32 || c == 127)
                return true;
        }
        // Handling non-ASCII characters (>127)
        else
        {
            // C1 control characters (128-159)
            if (c <= 159)
                return true;

            // Check for whitespace characters
            if (char.IsWhiteSpace(c))
                return true;

            // Check Unicode category for other characters
            UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(c);
            if (category == UnicodeCategory.Control ||
                category == UnicodeCategory.Format ||
                category == UnicodeCategory.Surrogate ||
                category == UnicodeCategory.PrivateUse ||
                category == UnassignedCategory)
            {
                return true;
            }
        }
    }

    return false;
}
