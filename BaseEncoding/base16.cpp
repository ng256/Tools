#include <iostream>
#include <iomanip>
#include <sstream>
#include <fstream>
#include <vector>
#include <string>
#include <cctype>
#include <csignal>
#include <algorithm>
#include <set>
#include <stdexcept>
#include "prettytable.h"

#ifdef _WIN32
#include <windows.h>
#include <io.h>
#define NEWLINE "\r\n"
#else
#include <sys/ioctl.h>
#include <unistd.h>
#define NEWLINE "\n"
#endif

#define STRLINE(str) (std::string(str) + NEWLINE)

// Constants
const int DEFAULT_CONSOLE_WIDTH = 80;
const int HEX_BYTE_LENGTH = 2;
const int SEPARATOR_LENGTH = 1;
const std::string VERSION = "1.0a";

// Structure to hold parameters for encoding/decoding
struct Parameters {
    bool encode_mode; // Flag to indicate encoding mode
    bool upper_case; // Flag to indicate uppercase hexadecimal digits
    char separator; // Single character separator
    std::string prefix; // Prefix for each byte
    std::string postfix; // Postfix for each byte
    std::string header; // Header for the entire output
    std::string footer; // Footer for the entire output
    bool suppress_last_postfix; // Flag to suppress postfix for the last byte
    int max_columns; // Maximum number of columns (bytes) per line
    int max_chars; // Maximum number of characters per line
    std::string file_extension; // File extension for output
};

bool is_stdin_redirected() {
#ifdef _WIN32
    return _isatty(_fileno(stdin)) == 0;
#else
    return isatty(fileno(stdin)) == 0;
#endif
}

// Function to print messages with line wrapping and handling of non-breaking spaces
void print_message(std::ostream& output, const std::string& message, int max_line_length) {
    std::istringstream iss(message);
    std::string word;
    std::string line;
    int current_length = 0;

    // Read each word from the message
    while (iss >> word) {
        // Replace '_' with ' ' and '^' with '\t'
        std::replace(word.begin(), word.end(), '_', ' ');
        std::replace(word.begin(), word.end(), '^', '\t');

        // Check if the current line exceeds the maximum line length
        if (current_length + word.length() + (line.empty() ? 0 : 1) > max_line_length) {
            output << line << std::endl;
            line.clear();
            current_length = 0;
        }

        // Add the word to the current line
        if (!line.empty()) {
            line += " ";
            current_length += 1;
        }
        line += word;
        current_length += word.length();
    }

    // Output the remaining line
    if (!line.empty()) {
        output << line << std::endl;
    }
}

// Function to print a separator line of '-' characters
void print_separator_line(std::ostream& output, int max_chars) {
    for (int i = 0; i < max_chars; ++i) {
        output << '-';
    }
    output << std::endl;
}

// Function to print help message
void print_help(const std::string& program_name, int max_line_length) {
    print_message(std::cout, program_name + " ver. " + VERSION, max_line_length);
    print_message(std::cout, "Copyright (C) 2024 Pavel_Bashkardin", max_line_length);
    print_message(std::cout, "Description:", max_line_length);
    print_message(std::cout, "The BASE16 program is a command-line utility for encoding and decoding data in hex (hexadecimal) format. It supports various parameters and keys for configuring the encoding and decoding process, as well as formatting the output in different programming languages.", max_line_length);
    print_separator_line(std::cout, max_line_length);

    print_message(std::cout, "Usage:", max_line_length);
    print_message(std::cout, program_name + " [-e|-encode|-d|-decode] [-u|-ucase|-l|-lcase] [-s|-separator_separator] [-prefix_prefix] [-postfix_postfix] [-header_header] [-footer_footer] [-lang|-language_language] [-t|-text_text|-f|-file_file|-o|-output_output|-c|-columns_columns|-i|-input] [-h|-help]", max_line_length);
    print_separator_line(std::cout, max_line_length);

    print_message(std::cout, "Options:", max_line_length);
    print_message(std::cout, "  -e, -encode^^Encode input data to hexadecimal format (default).", max_line_length);
    print_message(std::cout, "  -d, -decode^^Decode hexadecimal input data to binary format.", max_line_length);
    print_message(std::cout, "  -u, -ucase^^Use uppercase hexadecimal digits (default).", max_line_length);
    print_message(std::cout, "  -l, -lcase^^Use lowercase hexadecimal digits.", max_line_length);
    print_message(std::cout, "  -s, -separator^^Set a single character separator between bytes.", max_line_length);
    print_message(std::cout, "  -prefix^^^Set a prefix for each byte.", max_line_length);
    print_message(std::cout, "  -postfix^^Set a postfix for each byte.", max_line_length);
    print_message(std::cout, "  -header^^^Set a header for the entire output.", max_line_length);
    print_message(std::cout, "  -footer^^^Set a footer for the entire output.", max_line_length);
    print_message(std::cout, "  -lang, -language^Set language-specific settings.", max_line_length);
    print_message(std::cout, "  -t, -text^^Use the following text as input.", max_line_length);
    print_message(std::cout, "  -f, -file^^Use the following file as input.", max_line_length);
    print_message(std::cout, "  -o, -output^^Use the following file as output.", max_line_length);
    print_message(std::cout, "  -c, -columns^^Set the maximum number of columns per line.", max_line_length);
    print_message(std::cout, "  -i, -input^^Enable interactive input mode.", max_line_length);
    print_message(std::cout, "  -h, -help^^Display this help message.", max_line_length);
    print_separator_line(std::cout, max_line_length);

    print_message(std::cout, "Language-specific settings:", max_line_length);
    print_message(std::cout, "  c, cpp, cs, vb, py, asm, go, rs, swift, kt, java, dart, js, ts, rb, php, lua", max_line_length);
    print_separator_line(std::cout, max_line_length);

    print_message(std::cout, "Examples:", max_line_length);
    print_message(std::cout, program_name + " -e -u -s ' ' -prefix '0x' -postfix ',' -header 'const unsigned char data[] = {' -footer '};' -t 'Hello World'", max_line_length);
    print_message(std::cout, program_name + " -d -l -f input.txt -o output.bin", max_line_length);
    print_message(std::cout, program_name + " -e -lang cpp -t 'Hello World' -o output.cpp", max_line_length);
    print_separator_line(std::cout, max_line_length);
}

// Function to get the width of the console
int get_output_width() {
#ifdef _WIN32
    CONSOLE_SCREEN_BUFFER_INFO csbi;
    if (GetConsoleScreenBufferInfo(GetStdHandle(STD_OUTPUT_HANDLE), &csbi)) {
        return csbi.srWindow.Right - csbi.srWindow.Left + 1;
    } else {
        return DEFAULT_CONSOLE_WIDTH;
    }
#else
    struct winsize w;
    if (ioctl(STDOUT_FILENO, TIOCGWINSZ, &w) == 0) {
        return w.ws_col;
    } else {
        return DEFAULT_CONSOLE_WIDTH;
    }
#endif
}

// Function to calculate the maximum number of columns that fit within the max_chars limit
int calculate_max_columns(int max_chars, const std::string& prefix, const std::string& postfix, char separator) {
    int prefix_length = prefix.length();
    int postfix_length = postfix.length();

    // Calculate the total length per byte including prefix, byte, postfix, and separator
    int total_length_per_byte = prefix_length + HEX_BYTE_LENGTH + postfix_length + SEPARATOR_LENGTH;

    // Calculate the maximum number of columns that fit within the max_chars limit
    int max_columns = max_chars / total_length_per_byte;

    return max_columns;
}

// Function to encode input data to hexadecimal format
void encode(std::istream& input, std::ostream& output, const Parameters& params) {
    char byte;
    int column_count = 0;
    bool is_last_byte = false;

    // Read each byte from the input stream
    while (input.get(byte)) {
        is_last_byte = input.peek() == EOF;

        // Output the byte in hexadecimal format with the specified prefix, postfix, and separator
        output << params.prefix << std::hex << std::setw(HEX_BYTE_LENGTH) << std::setfill('0') << (params.upper_case ? std::uppercase : std::nouppercase) << static_cast<int>(static_cast<unsigned char>(byte));

        // Add postfix and separator if not the last byte and not the last column
        if (!is_last_byte || !params.suppress_last_postfix) {
            output << params.postfix;
            if (params.separator && column_count < params.max_columns - 1) {
                output << params.separator;
            }
        }

        column_count++;

        // Add a newline if the maximum number of columns is reached
        if (params.max_columns > 0 && column_count == params.max_columns && !is_last_byte) {
            output << std::endl;
            column_count = 0;
        }
    }

    // Add a newline if there are remaining columns
    if (column_count != 0) {
        output << std::endl;
    }
}

// Function to decode hexadecimal input data to binary format
void decode(std::istream& input, std::ostream& output, const Parameters& params) {
    std::string hex_string;
    char ch;
    bool use_separator = params.separator != '\0';

    // Read each character from the input stream
    while (input.get(ch)) {
        if (isspace(ch)) {
            continue; // Ignore spaces
        }
        if (use_separator && ch == params.separator) {
            continue; // Ignore separator
        }
        if (!isxdigit(ch)) {
            print_message(std::cerr, "Invalid character: " + std::string(1, ch), params.max_chars);
            exit(1);
        }

        // Accumulate hexadecimal digits
        hex_string += static_cast<char>(tolower(ch));

        // Convert to byte when two hexadecimal digits are accumulated
        if (hex_string.size() == HEX_BYTE_LENGTH) {
            unsigned int byte;
            std::stringstream hex_ss;
            hex_ss << std::hex << hex_string;
            hex_ss >> byte;
            output << static_cast<char>(byte);
            hex_string.clear();
        }
    }

    // Check for incomplete hexadecimal byte
    if (!hex_string.empty()) {
        print_message(std::cerr, "Incomplete hexadecimal byte: " + hex_string, params.max_chars);
        exit(1);
    }
}

// Function to handle input and determine whether to encode or decode
void handle_input(std::istream& input, std::ostream& output, const Parameters& params) {
    if (!params.header.empty()) {
        output << params.header;// << std::endl;
    }

    // Determine whether to encode or decode based on the encode_mode flag
    if (params.encode_mode) {
        encode(input, output, params);
    } else {
        decode(input, output, params);
    }

    if (!params.footer.empty()) {
        output << params.footer;
    }
    
    output << std::endl;
}

// Signal handler for interactive mode
void signal_handler(int signum) {
    std::cout << std::endl;
    exit(signum);
}

// Function to set language-specific settings
void set_language_settings(const std::string& lang, Parameters& params) {
    if (lang == "c") {
        // Settings for C language
        params.max_columns = 8;
        params.separator = ' ';
        params.prefix = "0x";
        params.postfix = ",";
        params.header = STRLINE("const unsigned char data[] = {");
        params.footer = STRLINE("};");
        params.suppress_last_postfix = true;
        params.file_extension = ".c";
    } else if (lang == "cpp") {
        // Settings for C++ language
        params.max_columns = 8;
        params.separator = ' ';
        params.prefix = "0x";
        params.postfix = ",";
        params.header = STRLINE("const std::vector<unsigned char> data = {");
        params.footer = STRLINE("};");
        params.suppress_last_postfix = true;
        params.file_extension = ".cpp";
    } else if (lang == "cs") {
        // Settings for C# language
        params.max_columns = 8;
        params.separator = ' ';
        params.prefix = "0x";
        params.postfix = ",";
        params.header = STRLINE("byte[] data = new byte[] {");
        params.footer = STRLINE("};");
        params.suppress_last_postfix = true;
        params.file_extension = ".cs";
    } else if (lang == "vb") {
        // Settings for Visual Basic language
        params.max_columns = 8;
        params.separator = ' ';
        params.prefix = "&H";
        params.postfix = ",";
        params.header = STRLINE("Dim data As Byte() = {");
        params.footer = STRLINE("}");
        params.suppress_last_postfix = true;
        params.file_extension = ".vb";
    } else if (lang == "py") {
        // Settings for Python language
        params.max_columns = 8;
        params.separator = ' ';
        params.prefix = "0x";
        params.postfix = ",";
        params.header = STRLINE("data = bytes([");
        params.footer = STRLINE("])");
        params.suppress_last_postfix = true;
        params.file_extension = ".py";
    } else if (lang == "asm") {
        // Settings for Assembly language
        params.max_columns = 8;
        params.separator = ' ';
        params.prefix = "0x";
        params.postfix = ",";
        params.header = STRLINE("data db ");
        params.footer = "";
        params.suppress_last_postfix = true;
        params.file_extension = ".asm";
    } else if (lang == "go") {
        // Settings for Go language
        params.max_columns = 8;
        params.separator = ' ';
        params.prefix = "0x";
        params.postfix = ",";
        params.header = STRLINE("var data = []byte{");
        params.footer = STRLINE("}");
        params.suppress_last_postfix = true;
        params.file_extension = ".go";
    } else if (lang == "rs") {
        // Settings for Rust language
        params.max_columns = 8;
        params.separator = ' ';
        params.prefix = "0x";
        params.postfix = ",";
        params.header = STRLINE("let data: [u8; N] = [");
        params.footer = STRLINE("];");
        params.suppress_last_postfix = true;
        params.file_extension = ".rs";
    } else if (lang == "swift") {
        // Settings for Swift language
        params.max_columns = 8;
        params.separator = ' ';
        params.prefix = "0x";
        params.postfix = ",";
        params.header = STRLINE("let data: [UInt8] = [");
        params.footer = STRLINE("]");
        params.suppress_last_postfix = true;
        params.file_extension = ".swift";
    } else if (lang == "kt") {
        // Settings for Kotlin language
        params.max_columns = 8;
        params.separator = ' ';
        params.prefix = "0x";
        params.postfix = ",";
        params.header = STRLINE("val data = byteArrayOf(");
        params.footer = STRLINE(")");
        params.suppress_last_postfix = true;
        params.file_extension = ".kt";
    } else if (lang == "java") {
        // Settings for Java language
        params.max_columns = 8;
        params.separator = ' ';
        params.prefix = "0x";
        params.postfix = ",";
        params.header = STRLINE("byte[] data = new byte[] {");
        params.footer = STRLINE("};");
        params.suppress_last_postfix = true;
        params.file_extension = ".java";
    } else if (lang == "dart") {
        // Settings for Dart language
        params.max_columns = 8;
        params.separator = ' ';
        params.prefix = "0x";
        params.postfix = ",";
        params.header = STRLINE("List<int> data = [");
        params.footer = STRLINE("];");
        params.suppress_last_postfix = true;
        params.file_extension = ".dart";
    } else if (lang == "js") {
        // Settings for JavaScript language
        params.max_columns = 8;
        params.separator = ' ';
        params.prefix = "0x";
        params.postfix = ",";
        params.header = STRLINE("const data = [");
        params.footer = STRLINE("];");
        params.suppress_last_postfix = true;
        params.file_extension = ".js";
    } else if (lang == "ts") {
        // Settings for TypeScript language
        params.max_columns = 8;
        params.separator = ' ';
        params.prefix = "0x";
        params.postfix = ",";
        params.header = STRLINE("const data: number[] = [");
        params.footer = STRLINE("];");
        params.suppress_last_postfix = true;
        params.file_extension = ".ts";
    } else if (lang == "rb") {
        // Settings for Ruby language
        params.max_columns = 8;
        params.separator = ' ';
        params.prefix = "0x";
        params.postfix = ",";
        params.header = STRLINE("data = [");
        params.footer = STRLINE("]");
        params.suppress_last_postfix = true;
        params.file_extension = ".rb";
    } else if (lang == "php") {
        // Settings for PHP language
        params.max_columns = 8;
        params.separator = ' ';
        params.prefix = "0x";
        params.postfix = ",";
        params.header = STRLINE("$data = [");
        params.footer = STRLINE("];");
        params.suppress_last_postfix = true;
        params.file_extension = ".php";
    } else if (lang == "lua") {
        // Settings for Lua language
        params.max_columns = 8;
        params.separator = ' ';
        params.prefix = "0x";
        params.postfix = ",";
        params.header = STRLINE("local data = {");
        params.footer = STRLINE("}");
        params.suppress_last_postfix = true;
        params.file_extension = ".lua";
    } else if (lang == "url") {
        // Settings for URL format
        params.max_columns = 8;
        params.separator = '\0'; // No separator
        params.prefix = "%"; // URL encoding prefix
        params.postfix = ""; // No postfix
        params.header = "http://";
        params.footer = "";
        params.suppress_last_postfix = false;
        params.file_extension = "";
        params.max_columns = 0;
    } else if (lang == "bat") {
        // Settings for BAT format
        params.max_columns = 8;
        params.separator = ' '; // No separator
        params.prefix = ""; // URL encoding prefix
        params.postfix = ""; // No postfix
        params.header = 
            STRLINE(":BEGIN\n") +
            STRLINE("@ECHO OFF") +
            STRLINE("SET /P filename=\"Enter filename: \"") +
            STRLINE("SET tmpfile=%~d0%~p0%RANDOM%.tmp") +
            STRLINE("SET outfile=%~d0%~p0%filename%") +
            STRLINE("ECHO tmpfile = %tmpfile%") +
            STRLINE("ECHO outfile = %outfile%") +
            STRLINE("FINDSTR \"^[0-9A-F][0-9A-F][^\\s]\" %0 > \"%tmpfile%\"") +
            STRLINE("certutil -decodehex \"%tmpfile%\" \"%outfile%\"") +
            STRLINE("TIMEOUT 3") +
            STRLINE("DEL /F /Q \"%tmpfile%\" %0") +
            STRLINE("EXIT") + NEWLINE;
        params.footer = "";
        params.suppress_last_postfix = false;
        params.file_extension = ".bat";
    } else {
        print_message(std::cerr, "Unknown language: " + lang, params.max_chars);
        exit(1);
    }
}


int main(int argc, char* argv[]) {
    Parameters params;
    params.encode_mode = true; // Default to encoding mode
    params.upper_case = true; // Default to uppercase
    params.separator = '\0'; // Default to no separator
    params.prefix = ""; // Prefix for each byte
    params.postfix = ""; // Postfix for each byte
    params.header = ""; // Header for the entire output
    params.footer = ""; // Footer for the entire output
    params.suppress_last_postfix = false; // Suppress postfix for the last byte
    std::istream* input = is_stdin_redirected() ? &std::cin : nullptr; // Default input from stdin
    std::ostream* output = &std::cout; // Default output to stdout
    std::string text_input; // For storing text after -t or -text option
    std::string file_name; // For storing file name after -f or -file option
    std::string output_file_name; // For storing output file name after -o or -output option
    bool interactive_mode = false; // Interactive input mode
    params.max_columns = 8; // Maximum number of columns (bytes) per line
    params.max_chars = get_output_width(); // Maximum number of characters per line
    // Calculate the maximum number of columns to fit within the max_chars limit
    params.max_columns = calculate_max_columns(params.max_chars, params.prefix, params.postfix, params.separator);

    std::set<std::string> seen_options;
    
    // Parse command-line arguments
    for (int i = 1; i < argc; ++i) {
        std::string arg = argv[i];
        std::transform(arg.begin(), arg.end(), arg.begin(), ::tolower); // Convert argument to lowercase
        bool has_next_arg = (i + 1 < argc);

        // Check for help option
        if (arg == "-h" || arg == "-help") {
            print_help(argv[0], params.max_chars);
            return 0;
        }

        // Check for encoding/decoding mode
        if (arg == "-d" || arg == "-decode") {
            if (seen_options.count("-e")) {
                print_message(std::cerr, "Conflicting options: -d/-decode and -e/-encode cannot be used together", params.max_chars);
                return 1;
            }
            params.encode_mode = false;
            seen_options.insert("-d");
        } else if (arg == "-e" || arg == "-encode") {
            if (seen_options.count("-d")) {
                print_message(std::cerr, "Conflicting options: -e/-encode and -d/-decode cannot be used together", params.max_chars);
                return 1;
            }
            params.encode_mode = true;
            seen_options.insert("-e");
        } else if (arg == "-l" || arg == "-lcase") {
            if (seen_options.count("-u")) {
                print_message(std::cerr, "Conflicting options: -l/-lcase and -u/-ucase cannot be used together", params.max_chars);
                return 1;
            }
            params.upper_case = false;
            seen_options.insert("-l");
        } else if (arg == "-u" || arg == "-ucase") {
            if (seen_options.count("-l")) {
                print_message(std::cerr, "Conflicting options: -u/-ucase and -l/-lcase cannot be used together", params.max_chars);
                return 1;
            }
            params.upper_case = true;
            seen_options.insert("-u");
        } else if (arg == "-s" || arg == "-separator") {
            if (seen_options.count("-s")) {
                print_message(std::cerr, "Duplicate option: -s/-separator", params.max_chars);
                return 1;
            }
            // Check for separator argument
            if (has_next_arg) {
                std::string separator_str = argv[++i];
                if (separator_str.length() == 1) {
                    params.separator = separator_str[0];
                } else {
                    print_message(std::cerr, "Separator must be a single character", params.max_chars);
                    return 1;
                }
            } else {
                params.separator = ' ';
            }
            seen_options.insert("-s");
        } else if (arg == "-prefix") {
            if (seen_options.count("-prefix")) {
                print_message(std::cerr, "Duplicate option: -prefix", params.max_chars);
                return 1;
            }
            // Check for prefix argument
            if (has_next_arg) {
                params.prefix = argv[++i];
            } else {
                print_message(std::cerr, "Missing prefix after -prefix option", params.max_chars);
                return 1;
            }
            seen_options.insert("-prefix");
        } else if (arg == "-postfix") {
            if (seen_options.count("-postfix")) {
                print_message(std::cerr, "Duplicate option: -postfix", params.max_chars);
                return 1;
            }
            // Check for postfix argument
            if (has_next_arg) {
                params.postfix = argv[++i];
            } else {
                print_message(std::cerr, "Missing postfix after -postfix option", params.max_chars);
                return 1;
            }
            seen_options.insert("-postfix");
        } else if (arg == "-header") {
            if (seen_options.count("-header")) {
                print_message(std::cerr, "Duplicate option: -header", params.max_chars);
                return 1;
            }
            // Check for header argument
            if (has_next_arg) {
                params.header = argv[++i];
            } else {
                print_message(std::cerr, "Missing header after -header option", params.max_chars);
                return 1;
            }
            seen_options.insert("-header");
        } else if (arg == "-footer") {
            if (seen_options.count("-footer")) {
                print_message(std::cerr, "Duplicate option: -footer", params.max_chars);
                return 1;
            }
            // Check for footer argument
            if (has_next_arg) {
                params.footer = argv[++i];
            } else {
                print_message(std::cerr, "Missing footer after -footer option", params.max_chars);
                return 1;
            }
            seen_options.insert("-footer");
        } else if (arg == "-lang" || arg == "-language") {
            if (seen_options.count("-lang")) {
                print_message(std::cerr, "Duplicate option: -lang/-language", params.max_chars);
                return 1;
            }
            // Check for language argument
            if (has_next_arg) {
                set_language_settings(argv[++i], params);
            } else {
                print_message(std::cerr, "Missing language after -lang/-language option", params.max_chars);
                return 1;
            }
            seen_options.insert("-lang");
            // Warn if redundant options are used with -lang
            if (seen_options.count("-prefix")) {
                print_message(std::cerr, "Warning: -prefix option is redundant when using -lang/-language", params.max_chars);
            }
            if (seen_options.count("-postfix")) {
                print_message(std::cerr, "Warning: -postfix option is redundant when using -lang/-language", params.max_chars);
            }
            if (seen_options.count("-header")) {
                print_message(std::cerr, "Warning: -header option is redundant when using -lang/-language", params.max_chars);
            }
            if (seen_options.count("-footer")) {
                print_message(std::cerr, "Warning: -footer option is redundant when using -lang/-language", params.max_chars);
            }
            if (seen_options.count("-s")) {
                print_message(std::cerr, "Warning: -s/-separator option is redundant when using -lang/-language", params.max_chars);
            }
        } else if (arg == "-t" || arg == "-text") {
            if (seen_options.count("-t")) {
                print_message(std::cerr, "Duplicate option: -t/-text", params.max_chars);
                return 1;
            }
            // Check for text input argument
            if (has_next_arg) {
                text_input = argv[++i];
                input = new std::istringstream(text_input);
            } else {
                print_message(std::cerr, "Missing text after -t/-text option", params.max_chars);
                return 1;
            }
            seen_options.insert("-t");
        } else if (arg == "-f" || arg == "-file") {
            if (seen_options.count("-f")) {
                print_message(std::cerr, "Duplicate option: -f/-file", params.max_chars);
                return 1;
            }
            // Check for file input argument
            if (has_next_arg) {
                file_name = argv[++i];
                input = new std::ifstream(file_name);
                if (!*input) {
                    print_message(std::cerr, "Failed to open file: " + file_name, params.max_chars);
                    return 1;
                }
            } else {
                print_message(std::cerr, "Missing file name after -f/-file option", params.max_chars);
                return 1;
            }
            seen_options.insert("-f");
        } else if (arg == "-o" || arg == "-output") {
            if (seen_options.count("-o")) {
                print_message(std::cerr, "Duplicate option: -o/-output", params.max_chars);
                return 1;
            }
            // Check for output file argument
            if (has_next_arg) {
                output_file_name = argv[++i];
                /*if (!params.file_extension.empty()) {
                    output_file_name += params.file_extension;
                }*/
                output = new std::ofstream(output_file_name);
                if (!*output) {
                    print_message(std::cerr, "Failed to open output file: " + output_file_name, params.max_chars);
                    return 1;
                }
            } else {
                print_message(std::cerr, "Missing output file name after -o/-output option", params.max_chars);
                return 1;
            }
            seen_options.insert("-o");
        } else if (arg == "-c" || arg == "-columns") {
            if (seen_options.count("-c")) {
                print_message(std::cerr, "Duplicate option: -c/-columns", params.max_chars);
                return 1;
            }
            // Check for columns argument
            if (has_next_arg) {
                try {
                    params.max_columns = std::stoi(argv[++i]);
                } catch (const std::invalid_argument& e) {
                    print_message(std::cerr, "Invalid argument for -c/-columns: " + std::string(argv[i]), params.max_chars);
                    return 1;
                } catch (const std::out_of_range& e) {
                    print_message(std::cerr, "Argument for -c/-columns out of range: " + std::string(argv[i]), params.max_chars);
                    return 1;
                }
            } else {
                print_message(std::cerr, "Missing number of columns after -c/-columns option", params.max_chars);
                return 1;
            }
            seen_options.insert("-c");
        } else if (arg == "-i" || arg == "-input") {
            if (seen_options.count("-i")) {
                print_message(std::cerr, "Duplicate option: -i/-input", params.max_chars);
                return 1;
            }
            // Enable interactive mode
            interactive_mode = true;
            signal(SIGINT, signal_handler);
            seen_options.insert("-i");
        } else {
            // Invalid argument
            print_message(std::cerr, "Invalid argument: " + arg, params.max_chars);
            print_help(argv[0], params.max_chars);
            return 1;
        }
    }
    try {
        if (interactive_mode) {
            std::stringstream buffer;
            std::string line;
            while (std::getline(std::cin, line)) {
                buffer << line << std::endl;
            }
            std::istringstream input_stream(buffer.str());
            handle_input(input_stream, *output, params);
            
        } else { if (input == nullptr){
		        print_help(argv[0], params.max_chars);
		        return 0;		
			}
            handle_input(*input, *output, params);
        }
    } catch (const std::exception& e) {
        print_message(std::cerr, "Error: " + std::string(e.what()), params.max_chars);
        return 1;
    }

    if (input != &std::cin) {
        delete input;
    }
    if (output != &std::cout) {
        delete output;
    }

    return 0;
}