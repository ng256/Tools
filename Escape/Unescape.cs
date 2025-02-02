// Converts any escaped characters in the input string.
string UnEscape(string text, params KeyValuePair<char, object>[] customEscapeCharacters)
{
	int pos = -1;
	int inputLength = text.Length;

	// Find the first occurrence of backslash or return the original text.
	for (int i = 0; i < inputLength; ++i) 
	{
		if (text[i] == '\\')
		{
			pos = i;
			break;
		}
	}

	if (pos < 0) return text; // Backslash not found.

	// If backslash is found.
	StringBuilder sb = new StringBuilder(text.Substring(0, pos));
	// [MethodImpl(MethodImplOptions.AggressiveInlining)] // Uncomment if necessary.
	char UnHex(string hex)
	{
		int c = 0;
		for (int i = 0; i < hex.Length; i++)
		{
			int r = hex[i];
			if (r > 0x2F && r < 0x3A) r -= 0x30;
			else if (r > 0x40 && r < 0x47) r -= 0x37;
			else if (r > 0x60 && r < 0x67) r -= 0x57;
			else throw new InvalidOperationException($"Unrecognized hexadecimal character {c} in \"{text}\".");
			c = c * 16 + r;
		}

		return (char)c;
	}

	do
	{
		char c = text[pos++];
		if (c == '\\')
		{
			c = pos < inputLength ? text[pos] 
				: throw new InvalidOperationException($"An_escape character was expected after the_last_backslash in {text}.");
			switch (c)
			{
				case '\\':
					c = '\\';
					break;
				case 'a':
					c = '\a';
					break;
				case 'b':
					c = '\b';
					break;
				case 'n':
					c = '\n';
					break;
				case 'r':
					c = '\r';
					break;
				case 'f':
					c = '\f';
					break;
				case 't':
					c = '\t';
					break;
				case 'v':
					c = '\v';
					break;
				case 'u' when pos < inputLength - 3:
					c = UnHex(text.Substring(++pos, 4));
					pos += 3;
					break;
				case 'x' when pos < inputLength - 1:
					c = UnHex(text.Substring(++pos, 2));
					pos++;
					break;
				case 'c' when pos < inputLength:
					c = text[++pos];
					if (c >= 'a' && c <= 'z')
						c -= ' ';
					if ((c = (char)(c - 0x40U)) >= ' ')
						throw new ArgumentException($"Unrecognized control character {c} in \"{text}\".", nameof(text));
					break;
				default:
					KeyValuePair<char, object> custom = customEscapeCharacters.FirstOrDefault(pair => pair.Key == c);
					sb.Append(custom.Value ?? 
						 throw new ArgumentException($"Unrecognized escape character \\{c} in \"{text}\".", nameof(text));
					pos++;
					continue;
			}
			pos++;
		}
		sb.Append(c);

	} while (pos < inputLength);

	return sb.ToString();
}
