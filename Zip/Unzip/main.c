#include <windows.h>
#include "unzip.h"

static void PrintHelp(void) {
    const char* help =
        "unzip - simple ZIP extractor (supports Store and Deflate)\n"
        "Usage: unzip.exe archive.zip [-d output_dir] [-v] [-h]\n"
        "Options:\n"
        "  archive.zip     ZIP file to extract\n"
        "  -d <dir>        extract to directory (default: current)\n"
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
    LPCWSTR zipFile = NULL;
    LPCWSTR outDir = NULL;
    BOOL verbose = FALSE;

    for (int i = 1; i < argc; ++i) {
        if (wcscmp(argv[i], L"-h") == 0 || wcscmp(argv[i], L"--help") == 0) {
            PrintHelp();
            return 0;
        } else if (wcscmp(argv[i], L"-v") == 0) {
            verbose = TRUE;
        } else if (wcscmp(argv[i], L"-d") == 0) {
            if (i + 1 < argc) {
                outDir = argv[++i];
            } else {
                PrintMessage(L"ERROR: -d requires a directory", TRUE);
                return 1;
            }
        } else if (zipFile == NULL && argv[i][0] != L'-') {
            zipFile = argv[i];
        } else {
            PrintMessage(L"ERROR: unknown option or extra argument", TRUE);
            PrintHelp();
            return 1;
        }
    }

    if (zipFile == NULL) {
        PrintMessage(L"ERROR: ZIP file not specified", TRUE);
        PrintHelp();
        return 1;
    }

    if (outDir == NULL) outDir = L".";
    PrintMessage(L"Extracting ZIP archive...", verbose);
    BOOL success = ExtractZipFile(zipFile, outDir, verbose);
    if (success) {
        PrintMessage(L"Extraction completed successfully.", verbose);
        return 0;
    } else {
        PrintMessage(L"Failed to extract archive.", verbose);
        return 1;
    }
}