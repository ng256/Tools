#pragma once
#include <windows.h>

// Flags for read options
#define INI_FLAG_NONE           0x0000
#define INI_FLAG_UNESCAPE       0x0001   // Replace escape sequences (\n, \r, \t, \\, \d, \t)
#define INI_FLAG_EXPAND_ENV     0x0002   // Expand environment variables %VAR%
#define INI_FLAG_CASE_SENSITIVE 0x0004   // Case-sensitive keys and sections

// Structure representing a parsed INI file (hidden implementation)
typedef struct IniFile IniFile;

// Parse INI data from a string (UTF-16)
// data      - string with INI text (does not need to be null-terminated, length specifies the length)
// length    - length of data in characters (if -1, then until null terminator)
// heap      - heap for memory allocation (if NULL, GetProcessHeap is used)
// flags     - combination of INI_FLAG_*
// returns NULL on error
IniFile* IniParse(const wchar_t* data, int length, HANDLE heap, DWORD flags);

// Free IniFile
void IniFree(IniFile* ini);

// Get list of sections (separator is null character, double null at the end)
// returns a pointer to a memory block with sections, or NULL
// the block size (in characters) is written to outSize (if not NULL)
// strings cannot be modified; memory belongs to IniFile
const wchar_t* IniGetSections(IniFile* ini, size_t* outSize);

// Get list of unique keys in the specified section (similar to above)
const wchar_t* IniGetKeys(IniFile* ini, const wchar_t* section, size_t* outSize);

// Get the first value for a key in a section (returns a string that does NOT need to be freed)
// if not found, returns NULL
const wchar_t* IniGetValue(IniFile* ini, const wchar_t* section, const wchar_t* key);

// Get all values for repeating keys in a section (array of strings, terminated by NULL)
// returns NULL if there are no values
// memory is freed along with IniFile, do not modify
const wchar_t** IniGetValues(IniFile* ini, const wchar_t* section, const wchar_t* key);