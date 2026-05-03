#include <windows.h>
#include "zip.h"

#define MAX_FILES 1024

static void PrintHelp(void) {
    const char* help =
        "zipmake - simple ZIP archiver (Store or Deflate compression)\n"
        "Usage: zipmake.exe -f file1 [file2 ...] -o archive.zip [-c comment.txt] [-deflate] [-store] [-v] [-h]\n"
        "Options:\n"
        "  -f <files>      input files (may appear multiple times)\n"
        "  -o <archive>    output ZIP file\n"
        "  -c <comment>    text file whose content becomes ZIP global comment\n"
        "  -deflate        use Deflate compression (default is Store)\n"
        "  -store          force Store method (no compression)\n"
        "  -v              verbose output\n"
        "  -h              this help\n";
    HANDLE hOut = GetStdHandle(STD_OUTPUT_HANDLE);
    DWORD written;
    WriteFile(hOut, help, lstrlenA(help), &written, NULL);
}

static void PrintMessage(const wchar_t* msg, BOOL verbose) {
    if (!verbose) return;
    HANDLE hOut = GetStdHandle(STD_OUTPUT_HANDLE);
    DWORD written;
    WriteFile(hOut, msg, wcslen(msg) * sizeof(wchar_t), &written, NULL);
    WriteFile(hOut, L"\n", 2, &written, NULL);
}

int wmain(int argc, wchar_t* argv[]) {
    wchar_t* files[MAX_FILES];
    int fileCount = 0;
    wchar_t* outputFile = NULL;
    wchar_t* commentFile = NULL;
    BOOL verbose = FALSE;
    BOOL useDeflate = FALSE;

    for (int i = 1; i < argc; ++i) {
        if (wcscmp(argv[i], L"-h") == 0 || wcscmp(argv[i], L"--help") == 0) {
            PrintHelp();
            return 0;
        } else if (wcscmp(argv[i], L"-v") == 0) {
            verbose = TRUE;
        } else if (wcscmp(argv[i], L"-deflate") == 0) {
            useDeflate = TRUE;
        } else if (wcscmp(argv[i], L"-store") == 0) {
            useDeflate = FALSE;
        } else if (wcscmp(argv[i], L"-f") == 0) {
            ++i;
            while (i < argc && argv[i][0] != L'-') {
                if (fileCount < MAX_FILES)
                    files[fileCount++] = argv[i];
                else {
                    PrintMessage(L"ERROR: too many input files", TRUE);
                    return 1;
                }
                ++i;
            }
            --i;
        } else if (wcscmp(argv[i], L"-o") == 0) {
            if (i + 1 < argc) {
                outputFile = argv[++i];
            } else {
                PrintMessage(L"ERROR: -o requires a filename", TRUE);
                return 1;
            }
        } else if (wcscmp(argv[i], L"-c") == 0) {
            if (i + 1 < argc) {
                commentFile = argv[++i];
            } else {
                PrintMessage(L"ERROR: -c requires a filename", TRUE);
                return 1;
            }
        } else {
            PrintMessage(L"ERROR: unknown option", TRUE);
            PrintHelp();
            return 1;
        }
    }

    if (fileCount == 0 || outputFile == NULL) {
        PrintMessage(L"ERROR: both -f and -o are required", TRUE);
        PrintHelp();
        return 1;
    }

    WORD compression = useDeflate ? ZIP_METHOD_DEFLATE : ZIP_METHOD_STORE;
    PrintMessage(useDeflate ? L"Using Deflate compression" : L"Using Store (no compression)", verbose);
    PrintMessage(L"Creating ZIP archive...", verbose);
    BOOL success = CreateZipFile(outputFile, (LPCWSTR*)files, fileCount, commentFile, compression);
    if (success) {
        PrintMessage(L"Archive created successfully.", verbose);
        return 0;
    } else {
        PrintMessage(L"Failed to create archive.", verbose);
        return 1;
    }
}