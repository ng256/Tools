using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Runtime.InteropServices;
using System.Threading;
using static System.Int32;
using static System.String;

namespace Base16
{
    public static class Program
    {
        #region Enums

        private enum Input : byte { Keyboard = 0, Text = 1, Files = 2, Pipe = 4, Unspecified = 0xFF }
        private enum Pattern : byte { Missing = 0, SFX = 1, C = 2, CS = 3, VB = 4 }
        private enum Mode : byte { Encode = 0, Decode = 1, Unspecified = 0xFF }
        private enum FileType { Unknown = 0, Disk = 1, Char = 2, Pipe = 3 } // "Pipe" if redirected std streams, "Char" otherwise.
        private enum StdHandle { StdIn = -10, StdOut = -11, StdErr = -12 } // Type of std stream.

        #endregion

        #region Global Parameters

        private const int WIDTH = 48; // Default line width.
        private static readonly Encoding TextEncoding =
            Encoding.GetEncoding(CultureInfo.CurrentCulture.TextInfo.OEMCodePage);

        private static readonly int ConsoleWidth = Console.WindowWidth;
        private static readonly string CommandLine = Environment.CommandLine;

        private class DateTimeNow { public override string ToString() => DateTime.Now.ToString(CultureInfo.InvariantCulture); }
        private static readonly KeyValuePair<char, object>[] CustomEscCharacters = new KeyValuePair<char, object>[]
        {
            new KeyValuePair<char, object>('l', "\r\n"),            // \l → new line
            new KeyValuePair<char, object>('d', new DateTimeNow()), // \d → current date and time.
        };

        /* Self-extracting command lines.
          :BEGIN  
          @ECHO OFF  
          SET /P filename="Enter filename: "  
          SET tmpfile=%~d0%~p0%RANDOM%.tmp  
          SET outfile=%~d0%~p0%filename%  
          ECHO tmpfile = %tmpfile%  
          ECHO outfile = %outfile%  
          FINDSTR "^[0-9A-F][0-9A-F][^\s]" %0 > "%tmpfile%"  
          certutil -decodehex "%tmpfile%" "%outfile%"  
          TIMEOUT 3  
          DEL /F /Q "%tmpfile%" %0  
          EXIT  
        */
        private static readonly byte[] SfxString = 
        {
            0x20, 0x20, 0x3A, 0x42, 0x45, 0x47, 0x49, 0x4E, 0x0D, 0x0A, 0x20,
            0x20, 0x40, 0x45, 0x43, 0x48, 0x4F, 0x20, 0x4F, 0x46, 0x46, 0x0D,
            0x0A, 0x20, 0x20, 0x53, 0x45, 0x54, 0x20, 0x2F, 0x50, 0x20, 0x66,
            0x69, 0x6C, 0x65, 0x6E, 0x61, 0x6D, 0x65, 0x3D, 0x22, 0x45, 0x6E,
            0x74, 0x65, 0x72, 0x20, 0x66, 0x69, 0x6C, 0x65, 0x6E, 0x61, 0x6D,
            0x65, 0x3A, 0x20, 0x22, 0x0D, 0x0A, 0x20, 0x20, 0x53, 0x45, 0x54,
            0x20, 0x74, 0x6D, 0x70, 0x66, 0x69, 0x6C, 0x65, 0x3D, 0x25, 0x7E,
            0x64, 0x30, 0x25, 0x7E, 0x70, 0x30, 0x25, 0x52, 0x41, 0x4E, 0x44,
            0x4F, 0x4D, 0x25, 0x2E, 0x74, 0x6D, 0x70, 0x0D, 0x0A, 0x20, 0x20,
            0x53, 0x45, 0x54, 0x20, 0x6F, 0x75, 0x74, 0x66, 0x69, 0x6C, 0x65,
            0x3D, 0x25, 0x7E, 0x64, 0x30, 0x25, 0x7E, 0x70, 0x30, 0x25, 0x66,
            0x69, 0x6C, 0x65, 0x6E, 0x61, 0x6D, 0x65, 0x25, 0x0D, 0x0A, 0x20,
            0x20, 0x45, 0x43, 0x48, 0x4F, 0x20, 0x74, 0x6D, 0x70, 0x66, 0x69,
            0x6C, 0x65, 0x20, 0x3D, 0x20, 0x25, 0x74, 0x6D, 0x70, 0x66, 0x69,
            0x6C, 0x65, 0x25, 0x0D, 0x0A, 0x20, 0x20, 0x45, 0x43, 0x48, 0x4F,
            0x20, 0x6F, 0x75, 0x74, 0x66, 0x69, 0x6C, 0x65, 0x20, 0x3D, 0x20,
            0x25, 0x6F, 0x75, 0x74, 0x66, 0x69, 0x6C, 0x65, 0x25, 0x0D, 0x0A,
            0x20, 0x20, 0x46, 0x49, 0x4E, 0x44, 0x53, 0x54, 0x52, 0x20, 0x22,
            0x5E, 0x5B, 0x30, 0x2D, 0x39, 0x41, 0x2D, 0x46, 0x5D, 0x5B, 0x30,
            0x2D, 0x39, 0x41, 0x2D, 0x46, 0x5D, 0x5B, 0x5E, 0x5C, 0x73, 0x5D,
            0x22, 0x20, 0x25, 0x30, 0x20, 0x3E, 0x20, 0x22, 0x25, 0x74, 0x6D,
            0x70, 0x66, 0x69, 0x6C, 0x65, 0x25, 0x22, 0x0D, 0x0A, 0x20, 0x20,
            0x63, 0x65, 0x72, 0x74, 0x75, 0x74, 0x69, 0x6C, 0x20, 0x2D, 0x64,
            0x65, 0x63, 0x6F, 0x64, 0x65, 0x68, 0x65, 0x78, 0x20, 0x22, 0x25,
            0x74, 0x6D, 0x70, 0x66, 0x69, 0x6C, 0x65, 0x25, 0x22, 0x20, 0x22,
            0x25, 0x6F, 0x75, 0x74, 0x66, 0x69, 0x6C, 0x65, 0x25, 0x22, 0x0D,
            0x0A, 0x20, 0x20, 0x54, 0x49, 0x4D, 0x45, 0x4F, 0x55, 0x54, 0x20,
            0x33, 0x0D, 0x0A, 0x20, 0x20, 0x44, 0x45, 0x4C, 0x20, 0x2F, 0x46,
            0x20, 0x2F, 0x51, 0x20, 0x22, 0x25, 0x74, 0x6D, 0x70, 0x66, 0x69,
            0x6C, 0x65, 0x25, 0x22, 0x20, 0x25, 0x30, 0x0D, 0x0A, 0x20, 0x20,
            0x45, 0x58, 0x49, 0x54, 0x0D, 0x0A, 0x0D, 0x0A
        };

        //private static bool _stdInput = false;    // Read data from standard input.
        private static bool _whiteSpace = false;    // Insert delimiters between bytes in output.
        private static bool _lcase = false;         // Convert output characters to lower case.
        private static char _dellimiter = ' ';      // Delimiter character.
        private static int _wrapWidth = MinValue;   // Wrap lines using current width.

        // Options.
        private static Pattern _pattern = Pattern.Missing; // Used for special output patterns.
        private static Mode _mode = Mode.Unspecified;      // Encode or decode.
        private static Input _input = Input.Unspecified;   // Program's input.

        // Escape-sensitive parameters.
        private static string _prefix = null;       // Prefix to every byte.
        private static string _postfix = null;      // Postfix for every byte except the last item.
        private static string _header = null;       // Header that is printed before the data is output.
        private static string _footer = null;       // Footer that is printed after the data is output.
        private static string _array = null;        // Name of the array being declared. 

        #endregion

        #region Encoding Stream Object

        // Represents stream that converts the original data to and from hexadecimal characters.
        private class HexEncodingStream : Stream
        {
            private readonly bool _leaveOpen = true;
            private int _linePosition = 0;
            private Stream _baseStream;
            private TextWriter _writer;
            private TextReader _reader;
            private int _itemLength;

            private HexEncodingStream() : base()
            {
                _itemLength = _whiteSpace ?  3 :  2;
                if (!IsNullOrEmpty(_prefix)) _itemLength += _prefix.Length;
                if (!IsNullOrEmpty(_postfix)) _itemLength += _postfix.Length;
            }

            public HexEncodingStream(Stream stream, bool leaveOpen = true) : this()
            {
                if (stream == null) throw new ArgumentNullException(nameof(stream));
                if (stream.CanRead) _reader = new StreamReader(stream);
                if (stream.CanWrite) _writer = new StreamWriter(stream);
                _baseStream = stream;
                _leaveOpen = leaveOpen;
            }

            public override void Flush()
            {
                _baseStream.Flush();
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                return _baseStream.Seek(offset, origin);
            }

            public override void SetLength(long value)
            {
                _baseStream.SetLength(value);
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                if (!CanRead) throw new NotSupportedException();
                int length = 0;
                for (int i = offset; i < offset + count; i++)
                {
                    int @byte = 0;
                    int digit = _reader.Read();
                    if (digit > 0x2F && digit < 0x3A) @byte = (digit - 0x30) << 4;
                    else if (digit > 0x40 && digit < 0x47) @byte = (digit - 0x37) << 4;
                    else if (digit > 0x60 && digit < 0x67) @byte = (digit - 0x57) << 4;
                    else break;
                    digit = _reader.Read();
                    if (digit > 0x2F && digit < 0x3A) @byte += digit - 0x30;
                    else if (digit > 0x40 && digit < 0x47) @byte += digit - 0x37;
                    else if (digit > 0x60 && digit < 0x67) @byte += digit - 0x57;
                    else break;
                    length++;
                    buffer[i] = unchecked((byte)@byte);
                }

                return length;
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                if (!CanWrite) throw new NotSupportedException();
                if (buffer == null) throw new ArgumentNullException(nameof(buffer));
                if (offset < 0) throw new ArgumentOutOfRangeException(nameof(buffer));
                if (count < 1) throw new ArgumentOutOfRangeException(nameof(buffer));

                int capacity = count * _itemLength;
                if (_wrapWidth > 2) capacity += capacity / _wrapWidth * 2;
                StringBuilder sb = new StringBuilder(capacity);
                for (int i = offset, limit = offset + count; i < limit; i++)
                {
                    byte b = buffer[i];

                    if (_prefix != null)
                    {
                        sb.Append(_prefix);
                        _linePosition += _prefix.Length;
                    }

                    int half = b / 16;
                    char digit;
                    digit = half < 10 ? (char) (half + 0x30) : (char) (_lcase ? half + 0x57 : half + 0x37);
                    sb.Append(digit);
                    half = b % 16;
                    digit = half < 10 ? (char) (half + 0x30) : (char) (_lcase ? half + 0x57 : half + 0x37);
                    sb.Append(digit);

                    _linePosition += 2;

                    if (i == limit - 1) break;

                    if (_postfix != null)
                    {
                        sb.Append(_postfix);
                        _linePosition += _postfix.Length;
                    }

                    if (_whiteSpace)
                    {
                        sb.Append(_dellimiter);
                        _linePosition++;
                    }

                    if (_wrapWidth > 1 && _linePosition >= _wrapWidth)
                    {
                        sb.Append("\r\n");
                        _linePosition = 0;
                    }
                }

                _writer.Write(sb.ToString());
            }

            public override bool CanRead => _baseStream.CanRead;

            public override bool CanSeek => _baseStream.CanSeek;

            public override bool CanWrite => _baseStream.CanWrite;

            public override long Length => _baseStream.Length;

            public override long Position
            {
                get => _baseStream.Position;
                set => _baseStream.Position = value;
            }

            protected override void Dispose(bool disposing)
            {
                _writer?.Flush();
                if (disposing && !_leaveOpen)
                {
                    _writer?.Close();
                    _reader?.Close();
                    _baseStream?.Close();
                }

                _writer = null;
                _reader = null;
                _baseStream = null;
            }
        }

        #endregion

        #region Win32 Imports

        [DllImport("kernel32.dll")]
        private static extern FileType GetFileType([In] IntPtr hdl);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetStdHandle([In] StdHandle std);

        [DllImport("kernel32.dll")]
        public static extern void ExitProcess([In] int exitCode);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        private static extern int GetModuleFileName([In] IntPtr hModule,
            [Out, MarshalAs(UnmanagedType.LPTStr)] string lpBuffer,
            [In, MarshalAs(UnmanagedType.I4)] int nSize);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        internal static extern int GetCurrentDirectory(
            [In, MarshalAs(UnmanagedType.I4)] int nSize,
            [Out, MarshalAs(UnmanagedType.LPTStr)] string lpBuffer);

        private static string GetModuleName()
        {
            string buffer = new string('\0', 260);
            int length = GetModuleFileName(IntPtr.Zero, buffer, 260);
            string moduleName = length > 0 
                ? Path.GetFullPath(buffer.Substring(0, length)) 
                : "base16.exe";
            return Path.GetFileNameWithoutExtension(moduleName).ToLowerInvariant();
        }

        private static string GetCurrentDirectory()
        {
            string buffer = new string('\0', 260);
            int length = GetCurrentDirectory(260, buffer);
            return length > 0 
                ? buffer.Substring(0, length) 
                : Path.GetDirectoryName(GetModuleName());
        }

        #endregion

        #region Additional Tools

        // Tries to perform the specified action or display an error message if it fails.
        private static bool TryAction(Action a, string message = null, int errorLevel = 0)
        {
            try
            {
                a?.Invoke();
                return true;
            }
            catch (Exception e)
            {
                using (StreamWriter errorWriter = new StreamWriter(Console.OpenStandardError(), TextEncoding))
                    errorWriter.WriteLine(WordWrap(IsNullOrEmpty(message)? e.Message : message, ConsoleWidth));
                if (errorLevel > 0)
                {
#if DEBUG
                    TryAction(() => throw new Exception("\r\nPress any key to end debugging..."));
                    Console.ReadKey(true);
#endif

                    ExitProcess(errorLevel);
                }
                return false;
            }
        }

        // Recursive file search.
        private static void SearchFiles(List<string> result, string directory)
        {
            string[] items = null;

            if (TryAction(() => items = Directory.GetFiles(directory)))
                result?.AddRange(items);

            if (TryAction(() => items = Directory.GetDirectories(directory)))
                foreach (string item in items)
                    SearchFiles(result, item);
        }

        // Searches for a file by mask.
        private static List<string> SearchFiles(string path)
        {
            List<string> result = new List<string>(255);
            if (Directory.Exists(path))
            {
                char last = path.Last();
                char altDirectorySeparatorChar = Path.AltDirectorySeparatorChar;
                char directorySeparatorChar = Path.DirectorySeparatorChar;
                if (last == altDirectorySeparatorChar)
                    path = path.Replace(altDirectorySeparatorChar, directorySeparatorChar);
                else if (last != directorySeparatorChar) path += directorySeparatorChar;
            }

            string searchPattern = Path.GetFileName(path);
            string directory = Path.GetDirectoryName(path);
            if (IsNullOrEmpty(directory)) directory = GetCurrentDirectory();
            if (IsNullOrEmpty(searchPattern)) SearchFiles(result, directory);
            else result.AddRange(Directory.GetFiles(directory, searchPattern));
            return result;
        }

        // Decodes hex dump to original data.
        private static void Decode(Stream input, Stream output)
        {
            using (Stream encodingStream = new HexEncodingStream(input, false))
                TryAction(() => encodingStream.CopyTo(output),
                    "Critical error: could not perform decoding! Exit process.", 1);
        }

        // Encodes input data to hex dump. 
        private static void Encode(Stream input, Stream output)
        {
            const string ERR_MSG = "Critical error: could not perform encoding! Exit process.";

            // Writes a header.
            if (!IsNullOrEmpty(_header))
            {

                byte[] bytes = TextEncoding.GetBytes(_header);
                TryAction(() => output.Write(bytes, 0, bytes.Length), ERR_MSG, 1);
            }

            // Encodes data.
            using (Stream encodingStream = new HexEncodingStream(output, true))
                TryAction(() => input?.CopyTo(encodingStream), ERR_MSG, 1);

            // Writes a footer.
            if (!IsNullOrEmpty(_footer))
            {
                byte[] bytes = TextEncoding.GetBytes(_footer);
                TryAction(() => output.Write(bytes, 0, bytes.Length), ERR_MSG, 1);
            }
        }

        // Gets the first several words from the summary.
        static string FirstWords(this string text, int words = 1)
        {
            if (IsNullOrEmpty(text)) return string.Empty;

            // Number of words we still want to display.
            if (words < 1) words = 1;

            // Loop through entire summary.
            for (int i = 0; i < text.Length; i++)
            {
                if (char.IsWhiteSpace(text[i])) words--;
                // If we have no more words to display, return the substring.
                if (words == 0) return text.Substring(0, i);
            }
            return string.Empty;
        }

        // This function wraps words. Using "_" means a non-breaking space in the text.
        private static string WordWrap(this string text, int width)
        {
            if (IsNullOrEmpty(text)) return string.Empty;

            int pos, next;
            StringBuilder sb = new StringBuilder();

            if (width < 1)
                return text;

            for (pos = 0; pos < text.Length; pos = next)
            {
                int eol = text.IndexOf(Environment.NewLine, pos, StringComparison.Ordinal);
                if (eol == -1)
                    next = eol = text.Length;
                else
                    next = eol + Environment.NewLine.Length;

                if (eol > pos)
                {
                    do
                    {
                        int len = eol - pos;
                        if (len > width)
                            len = BreakLine(text, pos, width);
                        sb.Append(text, pos, len);
                        sb.Append(Environment.NewLine);

                        pos += len;
                        while (pos < eol && char.IsWhiteSpace(text[pos]))
                            pos++;
                    } while (eol > pos);
                }
                else sb.Append(Environment.NewLine);
            }

            // The underline will be replaced by non-breaking spaces between words, which will not be wrapped.
            return sb.Replace('_', ' ').ToString();
        }

        // [MethodImpl(MethodImplOptions.AggressiveInlining)] // Uncomment if necessary.
        private static int BreakLine(string text, int pos, int max)
        {
            int i = max;
            while (i >= 0 && !char.IsWhiteSpace(text[pos + i]))
                i--;

            if (i < 0)
                return max;

            while (i >= 0 && char.IsWhiteSpace(text[pos + i]))
                i--;

            return i + 1;
        }

        // [MethodImpl(MethodImplOptions.AggressiveInlining)] // Uncomment if necessary.
        private static string PadRight(string s)
        {
            return s.PadRight(24, ' ');
        }

        // Displays a line that can be wrapped.
        private static void WriteLine(string s = null) 
        {
            if (s == null) Console.WriteLine();
            else Console.WriteLine(s.WordWrap(ConsoleWidth));
        }

        // Displays a two-column table, the second column can be wrapped.
        private static void WriteLine(string column1, string column2) 
        {
            Console.WriteLine(WordWrap(' ' + PadRight(column1) + column2, ConsoleWidth));
        }

        // Displays a center-aligned title.
        private static void WriteTitle(string s)
        {
            int length = s.Length + 2;
            int padWidth = (ConsoleWidth > length + 2 ? ConsoleWidth - length : 2) / 2;
            string p = Empty.PadRight(padWidth, '-');

            WriteLine($"{p} {s} {p}");

        }

        // Converts any escaped characters in the input string.
        private static string UnEscape(this string text, params KeyValuePair<char, object>[] customEscapeCharacters)
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
                    else throw new InvalidOperationException($"Unrecognized hexadecimal character {c} in \"{text}\"." +
                                                             $"\r\nThe_default value will_be_used.");
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
                        : throw new InvalidOperationException($"An_escape character was expected after the_last_backslash in {text}." +
                                                              $"\r\nThe_default value will_be_used.");
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
                                throw new InvalidOperationException($"Unrecognized control character {c} in \"{text}\"." +
                                                                    $"\r\nThe_default value will_be_used.");
                            break;
                        default:
                            KeyValuePair<char, object> custom = customEscapeCharacters.FirstOrDefault(pair => pair.Key == c);
                            sb.Append(custom.Value ?? 
                                 throw new InvalidOperationException($"Unrecognized escape character \\{c} in \"{text}\"." +
                                                                                              $"\r\nThe_default value will_be_used."));
                            pos++;
                            continue;
                    }
                    pos++;
                }
                sb.Append(c);

            } while (pos < inputLength);

            return sb.ToString();
        }

        private static string RemoveChars(this string text, params char[] deaf)
        {
            int textLength = text.Length;
            StringBuilder sb = null;
            //var except = new string(text.Except(deaf).ToArray());

            for (int i = 0; i < textLength; i++)
            {
                char c = text[i];
                for (int j = 0; j < deaf.Length; j++)
                {
                    if (c == deaf[j])
                    {
                        if (sb == null) sb = new StringBuilder(text.Substring(0, i));
                        goto next;
                    }
                }
                sb?.Append(c);
                next: ;
            }

            return sb == null ? text : sb.ToString();
        }

        #endregion

        #region Display Help

        static void WriteLittleHint()
        {
            WriteLine("There is no data for encode or decode. Exit process.\r\n"
                      + $"P.S. Type {GetModuleName()} without parameters to display help.");
#if DEBUG
            TryAction(() => throw new Exception("\r\nPress any key to end debugging..."));
            Console.ReadKey(true);
#endif
            ExitProcess(1);
        }

        static void WriteFullHelp()
        {
            string moduleName = GetModuleName();
            Assembly assembly = Assembly.GetExecutingAssembly();
            AssemblyName assemblyName = assembly.GetName();
            string version = assemblyName.Version.ToString();
#if DEBUG
            version += " (debug)";
#endif
            WriteLine();
            WriteLine($" Base-16 Encoding Utility v. {version}.\r\n Copyright: (C) 2019-{DateTime.Today.Year} NG256.");
            WriteLine();

            WriteLine($" Usage: {moduleName} [-e|-d] [-s] [-delimiter_char] [-prefix_prestr] [-postfix_poststr] [-header_hstr] [-footer_fstr] [-l] [-w_width] [-sfx] [-c] [-cs] [-vb] [-o_outfile] [-f]_file1_[file2...] [-t_text] [-i]");
            WriteLine();

            WriteTitle("Program operation mode");
            WriteLine("-e", "Encode data. This is default choise.");
            WriteLine("-d", "Decode data.");

            WriteTitle("Parameters that are used only for encoding");
            WriteLine("-s|-space", "Group bytes in the output with spaces.");
            WriteLine("-delimiter {char}", "Use the specified delimiter instead spaces. Used only with the_-s key.");
            WriteLine("-prefix {prestr}", "Use the specified prefix for every byte. This parameter is sensitive to escape characters.");
            WriteLine("-postfix {poststr}", "Use the specified postfix for every byte except the last item. " +
                                               "This parameter is sensitive to escape characters.");
            WriteLine("-header {hstr}", "Use the specified header for every byte except the last item. " +
                                             "This parameter is sensitive to escape characters.");
            WriteLine("-footer {fstr}", "Use the specified footer for every byte except the last item. " +
                                              "This parameter is sensitive to escape characters.");
            WriteLine("-l|-lcase", "Convert output to lowercase.");
            WriteLine("-w|-wrap {width}", "Split the_specified number of characters into lines. " +
                                          "A_value of this parameter less than 2 will_be ignored. By default, the output will not wrap.");
            WriteLine("-sfx", "Write a_special command lines before the encoded data to create a_self-extracting batch file. " +
                              "Items such as -s, -prefix, -postfix and -delimiter will be ignored.");
            WriteLine("-c", "Create an array declaration for a_C-like language. " +
                            "Items such as -s, -prefix, -postfix and -delimiter will be ignored.");

            WriteLine("-cs", "Create an array declaration for C# language. " +
                            "Items such as -s, -prefix, -postfix and -delimiter will be ignored.");

            WriteLine("-vb", "Create an array declaration for Visual_Basic language. " +
                            "Items such as -s, -prefix, -postfix and -delimiter will be ignored.");

            WriteTitle("Configuring input and output");
            WriteLine("-o|-output_{outfile}", "Set output to file {outfile}. " +
                                              "If parameter is_omitted, program's output will_be redirected to_the console window.");
            WriteLine("{file1}_{file2}_...", "Input files containing data to_be encoded.");
            WriteLine("-f|-file {value}", "Force use value as input filename (to escape parameters).");
            WriteLine(" If input files is omitted, program's input will be redirected to the standard input. " +
                      "Instead of a file name, you can specify a directory and file mask to search for files.");
            WriteLine("-t|-text {value}", "Use typed text value instead of input. This stuff should be after all other arguments. " +
                                          "This parameter is sensitive to escape characters.");
            WriteLine("-i|-input", "Read data from standard input device until Ctrl+C pressed. All listed files or key_-t will be ignored.");
            WriteLine("For redirected input from the output of another program, all listed files or text input will be ignored.");

            WriteTitle("Examples of using");
            WriteLine($"{moduleName} file1.txt", "\r\nWill display the encoded data of file1.txt.");
            WriteLine($"{moduleName} file1.txt -o file2.txt", "\r\nWill save the encoded data from file1.txt to file2.txt.");
            WriteLine($"{moduleName} -t Hello, world", "\r\nWill display: 48656C6C6F2C20776F726C64.");
            WriteLine($"{moduleName} -i", "\r\nWill input data from keyboard and encode it.");
            WriteLine($"{moduleName} -s file1.txt -o file2.txt", "\r\nWill save the encoded data from file1.txt to file2.txt, separated by bytes.");
            WriteLine($"{moduleName} -s -prefix 0x -postfix , -t Foo", "\r\nWill display: 0x42, 0x6F, 0x6F.");
            WriteLine($"{moduleName} -d -t 42 61 72", "\r\nWill display: Bar.");
            WriteLine($"{moduleName} -s -w 16 -l -delimiter ; test.txt",
                "\r\nWill display the encoded content of the file_\"test.txt\" with a_custom separator_\";\" " +
                "between bytes and wrap to a_lines width_of_16.");
            WriteLine($"{moduleName} -d encoded.txt -o original.txt",
                "\r\nOutput the decoded content of the file_\"encoded.txt\" to_a new file_\"original.txt\".");

#if DEBUG
            TryAction(() => throw new Exception("\r\nPress any key to end debugging..."));
            Console.ReadKey(true);
#endif
            ExitProcess(0);
        }

        #endregion

        // Program entry point.
        public static int Main(string[] args)
        {
#if DEBUG
            
            //goto exit;
#endif
            #region Settings

            int argc = args.Length;
            string outFileName = null;
            List<string> fileList = new List<string>();
            StringBuilder textBuffer = new StringBuilder(1024);
            if (GetFileType(GetStdHandle(StdHandle.StdIn)) != FileType.Char) _input = Input.Pipe;

            #endregion

            #region Command Line Args

            if (argc == 0 && _input != Input.Pipe) WriteFullHelp();

            const string ARG_ERR = "Critical error. Incorrect combination of options:\r\n"
                                   + "{0}_and_{1} cannot be together. Exit process";

            for (int i = 0; i < argc; i++)
            {
                string arg = args[i];
                bool notLast = i < argc - 1;
                char a = arg[0];
                if (a == '-' || a == '/')
                {
                    arg = arg.ToLowerInvariant().RemoveChars('/', '-');
                    switch (arg)
                    {
                        case "?":
                        case "h":
                        case "help":
                            WriteFullHelp();
                            continue;
                        case "i":
                        case "input":
                            if (_input != Input.Pipe)
                                TryAction(() =>
                                    _input = _input == Input.Unspecified
                                        ? Input.Keyboard
                                        : throw new Exception(Format(ARG_ERR, _input, Input.Keyboard)), errorLevel: 1);
                            continue;
                        case "sfx":
                        case "c":
                        case "cs":
                        case "vb":
                            TryAction(() =>
                            {
                                if (notLast)
                                {
                                    /*TODO: name of array*/
                                    string array = args[i + 1];
                                    if (arg != "sfx" && !array.StartsWith("-") && !array.StartsWith("/"))
                                    {
                                        _array = array;
                                        i++;
                                    }

                                }
                                if (Enum.TryParse(arg, true, out Pattern pattern))
                                    _pattern = _pattern == Pattern.Missing
                                        ? pattern
                                        : throw new Exception(Format(ARG_ERR, _pattern, pattern));
                            }, errorLevel: 1);
                            continue;
                        case "e":
                        case "encode":
                        case "d":
                        case "decode":
                            TryAction(() =>
                            {
                                if (arg == "e") arg = "encode";
                                if (arg == "d") arg = "decode";
                                if (Enum.TryParse(arg, true, out Mode mode))
                                    _mode = _mode == Mode.Unspecified
                                        ? mode
                                        : throw new Exception(Format(ARG_ERR, _mode, mode));
                            }, errorLevel: 1);
                            continue;
                        case "prefix" when notLast:
                            TryAction(() => _prefix = args[++i].UnEscape(CustomEscCharacters));
                            continue;
                        case "postfix" when notLast:
                            TryAction(() => _postfix = args[++i].UnEscape(CustomEscCharacters));
                            continue;
                        case "header" when notLast:
                            TryAction(() => _header = args[++i].UnEscape(CustomEscCharacters));
                            continue;
                        case "footer" when notLast:
                            TryAction(() => _footer = args[++i].UnEscape(CustomEscCharacters));
                            continue;
                        case "delimiter" when notLast:
                            arg = args[++i];
                            TryAction(() =>
                            {
                                _dellimiter = arg[0];
                                if (arg.Length > 1)
                                    WriteLine(
                                        $"It's too_long input line_\"{arg}\" for delimiter parameter. " +
                                        $"Only the_first character_\"{_dellimiter}\" will be used.");
                            }, "Wrong input line for delimiter parameter.");
                            goto case "space";
                        case "s":
                        case "space":
                            _whiteSpace = true;
                            continue;
                        case "l":
                        case "lcase":
                            _lcase = true;
                            continue;
                        case "w" when notLast:
                        case "wrap" when notLast:
                            arg = args[++i];
                            if (!TryAction(() =>
                                {
                                    _wrapWidth = Parse(arg, NumberStyles.Integer, CultureInfo.InvariantCulture);
                                    if (_wrapWidth < 2) throw new ArgumentOutOfRangeException(nameof(_wrapWidth), _wrapWidth, null);
                                },
                                $"Input string \"{arg}\" is not in_a_correct format for wrap width parameter.\r\nThe_default value will_be_used.")
                            )
                                _wrapWidth = MinValue;
                            continue;
                        case "o" when notLast:
                        case "out" when notLast:
                            arg = args[++i];
                            if (!TryAction(() =>
                                {
                                    if (!IsNullOrEmpty(outFileName)) throw new Exception();
                                    outFileName = Path.GetFullPath(arg);
                                },
                                $"Bad output file name: {arg}. The_default output will_be_used."))
                                outFileName = null;
                            continue;
                        case "t" when notLast:
                        case "text" when notLast:
                            if (_input != Input.Pipe && TryAction(() =>
                            {
                                if (_input != Input.Unspecified)
                                    throw new InvalidOperationException(
                                        Format(ARG_ERR, _input, Input.Text));
                                int startPos = CommandLine.IndexOf(arg, StringComparison.Ordinal) + arg.Length + 1;
                                string cmdLine = CommandLine.Substring(startPos);
                                textBuffer.Append(cmdLine.UnEscape(CustomEscCharacters));
                                _input = Input.Text;
                            }, errorLevel: 1)) goto start;
                            break;
                        case "f" when notLast:
                        case "file" when notLast:
                            arg = args[++i];
                            goto addPath;
                        default:
                            goto addPath;
                    }
                }

                addPath:
                if (_input == Input.Pipe) continue;

                TryAction(() =>
                {
                    if (_input != Input.Unspecified && _input != Input.Files)
                        throw new InvalidOperationException(
                            "Critical error. Incorrect combination of input options: "
                            + $"-{_input} and pathname {arg} cannot be together. Exit process");

                }, errorLevel: 1);
                TryAction(() =>
                {
                    List<string> list = SearchFiles(arg);
                    if (list.Count == 0) throw new FileNotFoundException(null, arg);
                    fileList.AddRange(list);
                    _input = Input.Files;
                }, $"Could not find the specified path: {arg}. This item will be skipped.");

            }

            #endregion

            #region Encoding/Decoding

            start:

            // Reads data from standard input until Ctrl+C pressed.
            TryAction(() =>
            {
                if (_input == Input.Keyboard)
                {
                    if (textBuffer.Length > 0) textBuffer.Clear();
                    using (TextWriter writer = new StreamWriter(Console.OpenStandardError(), TextEncoding))
                    {
                        Thread readKey = new Thread(() =>
                        {
                            writer.Write("Enter your text then press Ctrl+C to complete input:\r\n");
                            writer.Flush();
                            while (true)
                            {
                                ConsoleKeyInfo c = Console.ReadKey(true);
                                char keyChar = c.KeyChar;
                                if (keyChar == (char)8) keyChar = '\0';
                                switch (c.Key)
                                {
                                    case ConsoleKey.Enter: // Breaks a line.
                                        textBuffer.Append("\r\n");
                                        writer.WriteLine();
                                        writer.Flush();
                                        continue;
                                    case ConsoleKey.Backspace when Console.CursorLeft > 0 && textBuffer.Length > 0: // Erases a symbol.
                                        textBuffer.Remove(textBuffer.Length - 1, 1);
                                        Console.CursorLeft--;
                                        writer.Write('\0');
                                        writer.Flush();
                                        Console.CursorLeft--;
                                        continue;
                                    default: // It's for unprintable characters.
                                        if (keyChar == '\0')
                                            Console.Beep();
                                        else // Writes character on the screen.
                                        {
                                            textBuffer.Append(keyChar);
                                            writer.Write(keyChar);
                                            writer.Flush();
                                        }

                                        break;
                                }
                            }
                        });
                        ConsoleCancelEventHandler eventHandler = (sender, eventArgs) => // Waits for Ctrl+C, then terminates input. 
                        {
                            eventArgs.Cancel = true;
                            readKey.Abort();
                        };
                        Console.CancelKeyPress += eventHandler;
                        readKey.Start();
                        while (readKey.IsAlive) Thread.Sleep(10);
                        if (textBuffer.Length > 0) writer.WriteLine("\r\n");
                        Console.CancelKeyPress -= eventHandler;
                    }
                    _input = Input.Text;
                }
            }, "Critical error: could not open input device! Exit process.", 1);

            // Performs encoding or decoding.
            TryAction(() =>
            {
                using (Stream outStream = IsNullOrEmpty(outFileName)
                    ? Console.OpenStandardOutput()
                    : File.Create(outFileName, 4096, FileOptions.SequentialScan))
                    switch (_mode)
                    {
                        case Mode.Decode:

                            switch (_input)
                            {
                                // Decodes redirected input.
                                case Input.Pipe:
                                    TryAction(() => Decode(Console.OpenStandardInput(), outStream),
                                        "Critical error: could not open standard input device! Exit process.", 1);
                                    break;

                                // Decodes the text entered on the command line after argument -t.
                                case Input.Text when textBuffer.Length > 0:
                                    TryAction(() => Decode(new MemoryStream(TextEncoding.GetBytes(textBuffer.ToString())),
                                        outStream), "Critical error: cannot decode text buffer! Exit process.", 1);
                                    return;

                                // Decodes files from a file list.
                                case Input.Files when fileList.Count > 0:
                                    foreach (string fileName in fileList.Distinct())
                                        TryAction(() => Decode(File.OpenRead(fileName), outStream),
                                            $"Critical error: could not open input file: {fileName}! Exit process.", 1);
                                    return;

                                // If nothig have to do.
                                default:
                                    WriteLittleHint();
                                    return;
                            }

                            return;

                        default:

                            switch (_pattern)
                            {
                                // No patterns used.
                                case Pattern.Missing:
                                    break;

                                // Configures the output for SFX HEX dump.
                                case Pattern.SFX:
                                    _whiteSpace = true;
                                    _prefix = null;
                                    _postfix = null;
                                    _header = TextEncoding.GetString(SfxString);
                                    _footer = null;
                                    _dellimiter = ' ';
                                    _wrapWidth = _wrapWidth < 2 ? WIDTH : _wrapWidth;
                                    break;

                                // Configures the output for C-like array declaration.
                                case Pattern.C:
                                    _whiteSpace = true;
                                    _prefix = "0x";
                                    _postfix = ",";
                                    _header = IsNullOrEmpty(_array) ? string.Empty : $"unsigned char *{_array} = ";
                                    _header += "unsigned char[]\r\n" +
                                               "{\r\n";
                                    _footer = "\r\n}";
                                    _dellimiter = ' ';
                                    _wrapWidth = _wrapWidth < 2 ? WIDTH : _wrapWidth;
                                    break;

                                // Configures the output for CSharp array declaration.
                                case Pattern.CS:
                                    _whiteSpace = true;
                                    _prefix = "0x";
                                    _postfix = ",";
                                    _header = IsNullOrEmpty(_array) ? string.Empty : $"byte[] {_array} = ";
                                    _header += "new byte[]\r\n" +
                                               "{\r\n";
                                    _footer = "\r\n}";
                                    _dellimiter = ' ';
                                    _wrapWidth = _wrapWidth < 2 ? WIDTH : _wrapWidth;
                                    break;

                                // Configures the output for Visual Basic array declaration.
                                case Pattern.VB:
                                    _whiteSpace = true;
                                    _prefix = "&";
                                    _postfix = ",";
                                    _header = IsNullOrEmpty(_array) ? string.Empty : $"Dim {_array} = ";
                                    _header += "New Byte()\r\n"
                                               + "{\r\n";
                                    _footer = "\r\n}";
                                    _dellimiter = ' ';
                                    _wrapWidth = _wrapWidth < 2 ? WIDTH : _wrapWidth;
                                    break;
                            }

                            switch (_input)
                            {
                                // Encodes the standard input.
                                case Input.Pipe:
                                    TryAction(() => Encode(Console.OpenStandardInput(), outStream),
                                        "Critical error: could not open standard input device! Exit process.", 1);
                                    return;

                                // Encodes the text entered on the command line after argument -t.
                                case Input.Text when textBuffer.Length > 0:
                                    TryAction(() =>
                                        Encode(new MemoryStream(TextEncoding.GetBytes(textBuffer.ToString())),
                                            outStream), "Critical error: cannot encode text buffer! Exit process.", 1);
                                    return;

                                // Encodes files from a file list.
                                case Input.Files when fileList.Count > 0:
                                    foreach (string fileName in fileList.Distinct())
                                        TryAction(() => Encode(File.OpenRead(fileName), outStream),
                                            $"Critical error: could not open input file: {fileName}", 1);

                                    return;

                                // If nothig have to do.
                                default:
                                    WriteLittleHint();
                                    return;
                            }
                    }
            }, "Critical error: could not open output device! Exit process.", 1);

            #endregion

#if DEBUG
            exit:
            TryAction(() => throw new Exception("\r\nPress any key to end debugging..."));
            Console.ReadKey(true);
#endif

            return 0;
        }
    }
}
