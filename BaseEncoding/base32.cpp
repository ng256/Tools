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
const std::string VERSION = "1.0a";

// Structure to hold parameters for encoding/decoding
struct Parameters {
    bool encode_mode; // Flag to indicate encoding mode
    char separator; // Single character separator
    std::string header; // Header for the entire output
    std::string footer; // Footer for the entire output
    bool suppress_last_postfix; // Flag to suppress postfix for the last byte
    int max_columns; // Maximum number of columns (bytes) per line
    int max_chars; // Maximum number of characters per line
    std::string file_extension; // File extension for output
    bool trailing_chars; // Flag to include/exclude trailing characters
    bool lower_case; // Flag to encode in lower case
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
    print_message(std::cout, "The BASE32 program is a command-line utility for encoding and decoding data in Base32 format. It supports various parameters and keys for configuring the encoding and decoding process, as well as formatting the output.", max_line_length);
    print_separator_line(std::cout, max_line_length);

    print_message(std::cout, "Usage:", max_line_length);
    print_message(std::cout, program_name + " [-e|-encode|-d|-decode] [-s|-separator_separator] [-header_header] [-footer_footer] [-p|-padding] [-l|-lcase] [-text_text|-f|-file_file|-o|-output_output|-c|-columns_columns|-i|-input] [-h|-help]", max_line_length);
    print_separator_line(std::cout, max_line_length);

    print_message(std::cout, "Options:", max_line_length);
    print_message(std::cout, "  -e, -encode^^Encode input data to Base32 format (default).", max_line_length);
    print_message(std::cout, "  -d, -decode^^Decode Base32 input data to binary format.", max_line_length);
    print_message(std::cout, "  -s, -separator^^Set a single character separator between bytes.", max_line_length);
    print_message(std::cout, "  -header^^^Set a header for the entire output.", max_line_length);
    print_message(std::cout, "  -footer^^^Set a footer for the entire output.", max_line_length);
    print_message(std::cout, "  -p, -padding^^Include trailing characters.", max_line_length);
    print_message(std::cout, "  -l, -lcase^^Encode in lower case.", max_line_length);
    print_message(std::cout, "  -t, -text^^Use the following text as input.", max_line_length);
    print_message(std::cout, "  -f, -file^^Use the following file as input.", max_line_length);
    print_message(std::cout, "  -o, -output^^Use the following file as output.", max_line_length);
    print_message(std::cout, "  -c, -columns^^Set the maximum number of columns per line.", max_line_length);
    print_message(std::cout, "  -i, -input^^Enable interactive input mode.", max_line_length);
    print_message(std::cout, "  -h, -help^^Display this help message.", max_line_length);
    print_separator_line(std::cout, max_line_length);

    print_message(std::cout, "Examples:", max_line_length);
    print_message(std::cout, program_name + " -e -p -header_'-----BEGIN BASE32 ENCODED DATA-----' -footer_'-----END BASE32 ENCODED DATA-----' -f_data.bin -o encoded_data.txt", max_line_length);
    print_message(std::cout, program_name + " -d -f_encoded-data.txt -o_decoded-data.bin", max_line_length);
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
int calculate_max_columns(int max_chars, char separator) {
    // Calculate the total length per byte including byte and separator
    int total_length_per_byte = 8 + 1;

    // Calculate the maximum number of columns that fit within the max_chars limit
    int max_columns = max_chars - 1;

    return max_columns;
}

// Function to encode input data to Base32 format
void encode(std::istream& input, std::ostream& output, const Parameters& params) {
    static const char* base32_chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
    static const char* base32_chars_lower = "abcdefghijklmnopqrstuvwxyz234567";
    char byte;
    int column_count = 0;
    bool is_last_byte = false;
    std::string encoded;
    int val = 0, valb = -5;

    const char* base_chars = params.lower_case ? base32_chars_lower : base32_chars;

    // Read each byte from the input stream
    while (input.get(byte)) {
        val = (val << 8) + static_cast<unsigned char>(byte);
        valb += 8;
        while (valb >= 0) {
            encoded.push_back(base_chars[(val >> valb) & 0x1F]);
            valb -= 5;
        }
    }

    if (valb > -5) {
        encoded.push_back(base_chars[((val << 5) >> (valb + 5)) & 0x1F]);
    }

    if (params.trailing_chars) {
        while (encoded.size() % 8) {
            encoded.push_back('=');
        }
    }

    for (size_t i = 0; i < encoded.size(); ++i) {
        is_last_byte = (i == encoded.size() - 1);

        // Output the byte in Base32 format with the specified separator
        output << encoded[i];

        // Add separator if not the last byte and not the last column
        if (!is_last_byte && params.separator && column_count < params.max_columns - 1) {
            output << params.separator;
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

// Function to decode Base32 input data to binary format
void decode(std::istream& input, std::ostream& output, const Parameters& params) {
    static const int8_t base32_reverse_table[256] = {
        -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        -1, -1, 26, 27, 28, 29, 30, 31, -1, -1, -1, -1, -1, -1, -1, -1,
        -1,  0,  1,  2,  3,  4,  5,  6,  7,  8,  9, 10, 11, 12, 13, 14,
        15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, -1, -1, -1, -1, -1,
        -1,  0,  1,  2,  3,  4,  5,  6,  7,  8,  9, 10, 11, 12, 13, 14,
        15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, -1, -1, -1, -1, -1,
        -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1
    };

    char ch;
    int val = 0, valb = -5;
    std::string decoded;

    // Read each character from the input stream
    while (input.get(ch)) {
        if (isspace(ch)) {
            continue; // Ignore spaces
        }
        if (ch == '=') {
            break; // Stop at padding
        }
        if (base32_reverse_table[static_cast<unsigned char>(toupper(ch))] == -1) {
            print_message(std::cerr, "Invalid character: " + std::string(1, ch), params.max_chars);
            exit(1);
        }

        val = (val << 5) + base32_reverse_table[static_cast<unsigned char>(toupper(ch))];
        valb += 5;
        if (valb >= 8) {
            decoded.push_back(static_cast<char>((val >> (valb - 8)) & 0xFF));
            valb -= 8;
        }
    }

    output << decoded;
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

int main(int argc, char* argv[]) {
    Parameters params;
    params.encode_mode = true; // Default to encoding mode
    params.separator = '\0'; // Default to no separator
    params.header = ""; // Header for the entire output
    params.footer = ""; // Footer for the entire output
    params.suppress_last_postfix = false; // Suppress postfix for the last byte
    params.trailing_chars = false; // Default to exclude trailing characters
    params.lower_case = false; // Default to upper case
    std::istream* input = is_stdin_redirected() ? &std::cin : nullptr; // Default input from stdin
    std::ostream* output = &std::cout; // Default output to stdout
    std::string text_input; // For storing text after -t or -text option
    std::string file_name; // For storing file name after -f or -file option
    std::string output_file_name; // For storing output file name after -o or -output option
    bool interactive_mode = false; // Interactive input mode
    params.max_columns = 16; // Maximum number of columns (bytes) per line
    params.max_chars = get_output_width(); // Maximum number of characters per line
    // Calculate the maximum number of columns to fit within the max_chars limit
    params.max_columns = calculate_max_columns(params.max_chars, params.separator);

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
        } else if (arg == "-p" || arg == "-padding") {
            if (seen_options.count("-p")) {
                print_message(std::cerr, "Duplicate option: -p/-padding", params.max_chars);
                return 1;
            }
            params.trailing_chars = true;
            seen_options.insert("-p");
        } else if (arg == "-l" || arg == "-lcase") {
            if (seen_options.count("-l")) {
                print_message(std::cerr, "Duplicate option: -l/-lcase", params.max_chars);
                return 1;
            }
            params.lower_case = true;
            seen_options.insert("-l");
        } else if (arg == "-t" || arg == "-text") {
            if (seen_options.count("-t")) {
                print_message(std::cerr, "Duplicate option: -text", params.max_chars);
                return 1;
            }
            // Check for text input argument
            if (has_next_arg) {
                text_input = argv[++i];
                input = new std::istringstream(text_input);
            } else {
                print_message(std::cerr, "Missing text after -text option", params.max_chars);
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

        } else {
            if (input == nullptr){
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
