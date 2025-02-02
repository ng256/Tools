/***************************************************************
 *
 * File: BytesEncoding.cs
 *
 * Description:
 *
 * Represents of encoding styles. Each enumeration element
 * represents a specific bytes encoding style.
 *
 ***************************************************************/

namespace System.Ini
{
    /// <summary>
    ///     Represents the different encoding styles that can be used to
    ///     represent an array of bytes.
    /// </summary>
    public enum BytesEncoding
    {
        /// <summary>
        ///     Represents the Binary encoding style, where each byte is 
        ///     represented by its binary form (0s and 1s).
        /// </summary>
        Binary,

        /// <summary>
        ///     Represents the Octal encoding style, where each byte is 
        ///     represented by its octal form (digits 0-7).
        /// </summary>
        Octal,

        /// <summary>
        ///     Represents the Hexadecimal encoding style, where each byte
        ///     is represented by a pair of hexadecimal digits (0-9, A-F).
        /// </summary>
        Hexadecimal,

        /// <summary>
        ///     Represents the Base32 encoding style, where each 5 bits of
        ///     the original data is represented by a single character
        ///     from the Base32 character set (A-Z, 2-7).
        /// </summary>
        Base32,

        /// <summary>
        ///     Represents the Base64 encoding style, where each 6 bits of
        ///     the original data is represented by a single character
        ///     from the Base64 character set (A-Z, a-z, 0-9, +, /).
        /// </summary>
        Base64
    }
}
