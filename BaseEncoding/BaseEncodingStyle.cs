/***************************************************************************************

• File: BaseEncodingStyle.cs

• Description:

  Represents of encoding styles. Each enumeration element represents a specific bytes 
  encoding style.

•   MIT License

Copyright © Pavel Bashkardin, 2024

Permission  is  hereby  granted,  free  of  charge,  to  any  person  obtaining  a  copy
of  this  software  and  associated  documentation  files  (the  "Software"),  to  deal
in  the  Software  without  restriction,  including  without  limitation  the  rights to
use,  copy,  modify,  merge,  publish,  distribute,  sublicense,  and/or  sell copies of
the  Software,  and  to  permit  persons  to  whom  the  Software  is  furnished  to  do
so, subject to the following conditions:

The  above  copyright  notice  and  this  permission  notice  shall  be  included in all
copies or substantial portions of the Software.

THE  SOFTWARE  IS  PROVIDED  "AS  IS",  WITHOUT  WARRANTY  OF  ANY  KIND,  EXPRESS   OR
IMPLIED,  INCLUDING  BUT  NOT  LIMITED  TO  THE  WARRANTIES  OF  MERCHANTABILITY, FITNESS
FOR  A  PARTICULAR  PURPOSE  AND  NONINFRINGEMENT.  IN  NO  EVENT  SHALL  THE  AUTHORS OR
COPYRIGHT  HOLDERS  BE  LIABLE  FOR  ANY  CLAIM,  DAMAGES  OR  OTHER  LIABILITY,  WHETHER
IN  AN  ACTION  OF  CONTRACT,  TORT  OR  OTHERWISE,  ARISING  FROM,  OUT  OF  OR   IN
CONNECTION  WITH  THE  SOFTWARE  OR  THE  USE  OR  OTHER  DEALINGS  IN  THE  SOFTWARE.

****************************************************************************************/

namespace System.Text
{
    /// <summary>
    ///     Represents the different encoding styles that can be used to
    ///     represent an array of bytes.
    /// </summary>
    public enum BaseEncodingStyle
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