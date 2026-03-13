#ifndef COPILOT_REMOVE_H
#define COPILOT_REMOVE_H

#include <windows.h>
#include <stdarg.h>
#include <strsafe.h>

// Line terminator
#define EOL "\r\n"

// Portable va_copy for MSVC and GCC
#if defined(_MSC_VER)
    #define VA_COPY(dest, src) (dest = src)
#elif defined(__GNUC__)
    #define VA_COPY(dest, src) __va_copy(dest, src)
#else
    #define VA_COPY(dest, src) (dest = src)
#endif

// STD streams global handles (defined in .c)
extern HANDLE g_hStdOut;
extern HANDLE g_hStdErr;
extern HANDLE g_hStdIn;

// Function declarations
void PrintFormat(HANDLE hOut, const char* fmt, va_list args);
void PrintColored(HANDLE hOut, WORD color, const char* fmt, va_list args);
void Print(const char* fmt, ...);
void PrintWarning(const char* fmt, ...);
void PrintSuccess(const char* fmt, ...);
void PrintError(const char* fmt, ...);
void ReadKey(void);
BOOL IsArgPresent(LPCSTR lpCmdLine, LPCSTR arg);
BOOL IsAdmin(void);
BOOL RunCommand(const char* command);
BOOL SetDWORD(HKEY root, const char* path, const char* name, DWORD value);
void RebootSystem(void);

#endif // COPILOT_REMOVE_H