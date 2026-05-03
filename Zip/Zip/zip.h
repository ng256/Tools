#pragma once
#include <windows.h>

// Compression methods
#define ZIP_METHOD_STORE   0
#define ZIP_METHOD_DEFLATE 8

BOOL CreateZipFile(
    LPCWSTR          zipFileName,
    LPCWSTR*         inputFiles,
    int              fileCount,
    LPCWSTR          globalCommentFile,
    WORD             compressionMethod   // ZIP_METHOD_STORE or ZIP_METHOD_DEFLATE
);
