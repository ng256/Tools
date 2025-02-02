/***************************************************************

•   File: InternalBaseEncoding.cs

•   Description.

    It is  an abstract  class  InternalBaseEncoding, which  is a
    descendant   of the    Encoding  class  and   implements the
    IBaseEncoding  interface for creating various encodings.

***************************************************************/

using System.Ini;
using static System.InternalTools;

namespace System.Text
{
  internal abstract class BaseEncoding : Encoding
  {
    protected BaseEncoding(int codePage)
      : base(codePage)
    {
    }

    public static BaseEncoding GetEncoding(BytesEncoding encoding)
    {
        switch (encoding)
        {
            case BytesEncoding.Binary:
                return new Base2Encoding();
            case BytesEncoding.Octal:
                return new Base8Encoding();
            case BytesEncoding.Hexadecimal:
                return new Base16Encoding();
            case BytesEncoding.Base32:
                return new Base32Encoding();
            case BytesEncoding.Base64:
                return new Base64Encoding();
                default:
                throw new ArgumentOutOfRangeException(nameof(encoding), encoding, null);
        }
    }

    public override int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex)
    {
        if (chars == null)
            throw new ArgumentNullException(nameof(chars));
        if (charIndex < 0)
            throw new ArgumentOutOfRangeException(nameof (charIndex), charIndex, GetResourceString("ArgumentOutOfRange_StartIndex"));
        if (charCount < 0)
            throw new ArgumentOutOfRangeException(nameof (charCount), charCount, GetResourceString("ArgumentOutOfRange_NegativeCount"));
        if (bytes == null)
            throw new ArgumentNullException(nameof (bytes));
        if (byteIndex < 0)
            throw new ArgumentOutOfRangeException(nameof (byteIndex), byteIndex, GetResourceString("ArgumentOutOfRange_StartIndex"));

        return 0;
    }

    public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex)
    {
        if (bytes == null)
            throw new ArgumentNullException(nameof (bytes));
        if (byteCount < 0)
            throw new ArgumentOutOfRangeException(nameof (byteCount), byteCount, GetResourceString("ArgumentOutOfRange_NegativeCount"));
        if (byteIndex < 0)
            throw new ArgumentOutOfRangeException(nameof (byteIndex), byteIndex, GetResourceString("ArgumentOutOfRange_StartIndex"));
        if (chars == null)
            throw new ArgumentNullException(nameof (chars));
        if (charIndex < 0)
            throw new ArgumentOutOfRangeException(nameof (charIndex), charIndex, GetResourceString("ArgumentOutOfRange_StartIndex"));

        return 0;
    }

    public override int GetByteCount(char[] chars, int index, int count)
    {
        return GetMaxByteCount(chars.GetMaxCount(index, count));
    }

    public override int GetCharCount(byte[] bytes, int index, int count)
    {
        return GetMaxCharCount(bytes.GetMaxCount(index, count));
    }

    protected virtual int GetCharCount(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex)
    {
      int maxCharCount = GetMaxCharCount(bytes.GetMaxCount(byteIndex));
      return Math.Min(chars.GetMaxCount(charIndex, charCount), maxCharCount);
    }

    protected virtual int GetByteCount(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex)
    {
      int maxByteCount = GetMaxByteCount(chars.GetMaxCount(charIndex));
      byteCount = Math.Min(bytes.GetMaxCount(byteIndex, byteCount), maxByteCount);
      return byteCount;
    }
  }
}
