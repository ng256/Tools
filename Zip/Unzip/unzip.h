#pragma once
#include <windows.h>

BOOL ExtractZipFile(
    LPCWSTR zipFileName,
    LPCWSTR outputDir,
    BOOL verbose
);