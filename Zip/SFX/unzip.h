#pragma once
#include <windows.h>

#pragma pack(push, 1)
typedef struct {
    DWORD signature;
    WORD  diskNumber;
    WORD  centralDirDiskStart;
    WORD  entriesOnThisDisk;
    WORD  totalEntries;
    DWORD centralDirSize;
    DWORD centralDirOffset;
    WORD  commentLength;
} EndOfCentralDirectory;
#pragma pack(pop)

BOOL ExtractFromSFX(LPCWSTR exePath, LPCWSTR destDir, BOOL verbose, HWND hwndStatus);
BOOL FindEOCD(HANDLE hFile, DWORD fileSize, EndOfCentralDirectory* out, DWORD* outOffset);
BOOL GetZipComment(LPCWSTR exePath, wchar_t** commentOut, HANDLE heap);