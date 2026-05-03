#include "unzip.h"
#include <windows.h>

#pragma pack(push, 1)
typedef struct {
    DWORD signature;
    WORD  versionNeeded;
    WORD  flags;
    WORD  compression;
    WORD  modTime;
    WORD  modDate;
    DWORD crc32;
    DWORD compressedSize;
    DWORD uncompressedSize;
    WORD  fileNameLength;
    WORD  extraFieldLength;
} LocalFileHeader;

typedef struct {
    DWORD signature;
    WORD  versionMadeBy;
    WORD  versionNeeded;
    WORD  flags;
    WORD  compression;
    WORD  modTime;
    WORD  modDate;
    DWORD crc32;
    DWORD compressedSize;
    DWORD uncompressedSize;
    WORD  fileNameLength;
    WORD  extraFieldLength;
    WORD  fileCommentLength;
    WORD  diskNumberStart;
    WORD  internalAttributes;
    DWORD externalAttributes;
    DWORD localHeaderOffset;
} CentralDirectoryFileHeader;
#pragma pack(pop)

// ---------- Inflate (Fixed Huffman) ----------
#define WSIZE 32768

typedef struct {
    BYTE* data;
    DWORD size;
    DWORD pos;
} InStream;

typedef struct {
    BYTE* data;
    DWORD capacity;
    DWORD size;
    HANDLE heap;
} OutStream;

typedef struct HuffNode {
    struct HuffNode* child[2];
    int symbol;
} HuffNode;

static HuffNode* huffNodeAlloc(HANDLE heap) {
    HuffNode* node = (HuffNode*)HeapAlloc(heap, HEAP_ZERO_MEMORY, sizeof(HuffNode));
    node->child[0] = node->child[1] = NULL;
    node->symbol = -1;
    return node;
}

static void huffTreeFree(HuffNode* node, HANDLE heap) {
    if (!node) return;
    huffTreeFree(node->child[0], heap);
    huffTreeFree(node->child[1], heap);
    HeapFree(heap, 0, node);
}

static DWORD ReadBits(InStream* in, int bits, DWORD* bitBuf, int* bitCnt) {
    DWORD value = 0;
    for (int i = 0; i < bits; i++) {
        if (*bitCnt == 0) {
            if (in->pos >= in->size) return (DWORD)-1;
            *bitBuf = in->data[in->pos++];
            *bitCnt = 8;
        }
        value |= ((*bitBuf & 1) << i);
        *bitBuf >>= 1;
        (*bitCnt)--;
    }
    return value;
}

static void OutStream_Write(OutStream* out, BYTE b) {
    if (out->size >= out->capacity) {
        DWORD newCap = out->capacity * 2;
        BYTE* newData = (BYTE*)HeapReAlloc(out->heap, 0, out->data, newCap);
        if (!newData) return;
        out->data = newData;
        out->capacity = newCap;
    }
    out->data[out->size++] = b;
}

static HuffNode* BuildFixedTree(HANDLE heap) {
    BYTE lengths[288];
    int i;
    for (i = 0; i < 144; i++) lengths[i] = 8;
    for (i = 144; i < 256; i++) lengths[i] = 9;
    for (i = 256; i < 280; i++) lengths[i] = 7;
    for (i = 280; i < 288; i++) lengths[i] = 8;

    int bl_count[16] = {0};
    for (i = 0; i < 288; i++) bl_count[lengths[i]]++;
    int code = 0;
    int next_code[16] = {0};
    for (int bits = 1; bits <= 15; bits++) {
        code = (code + bl_count[bits-1]) << 1;
        next_code[bits] = code;
    }
    WORD codes[288];
    for (i = 0; i < 288; i++) {
        int len = lengths[i];
        if (len != 0)
            codes[i] = (WORD)next_code[len]++;
    }

    HuffNode* root = huffNodeAlloc(heap);
    for (i = 0; i < 288; i++) {
        if (lengths[i] == 0) continue;
        WORD rev = 0;
        WORD c = codes[i];
        int len = lengths[i];
        for (int j = 0; j < len; j++) {
            rev = (rev << 1) | (c & 1);
            c >>= 1;
        }
        HuffNode* node = root;
        for (int bit = len-1; bit >= 0; bit--) {
            int b = (rev >> bit) & 1;
            if (!node->child[b])
                node->child[b] = huffNodeAlloc(heap);
            node = node->child[b];
        }
        node->symbol = i;
    }
    return root;
}

static int DecodeLiteralLength(HuffNode* tree, InStream* in, DWORD* bitBuf, int* bitCnt) {
    HuffNode* node = tree;
    while (node->symbol == -1) {
        int bit = (int)ReadBits(in, 1, bitBuf, bitCnt);
        if (bit == -1) return -1;
        node = node->child[bit];
        if (!node) return -1;
    }
    return node->symbol;
}

static DWORD InflateData(BYTE* compressed, DWORD compSize, BYTE** output, HANDLE heap) {
    InStream in = { compressed, compSize, 0 };
    DWORD bitBuf = 0;
    int bitCnt = 0;
    OutStream out = { NULL, 4096, 0, heap };
    out.data = (BYTE*)HeapAlloc(heap, 0, out.capacity);

    HuffNode* litlenTree = BuildFixedTree(heap);
    BYTE window[WSIZE];
    DWORD windowFill = 0;

    DWORD bfinal = ReadBits(&in, 1, &bitBuf, &bitCnt);
    DWORD btype = ReadBits(&in, 2, &bitBuf, &bitCnt);
    if (bfinal != 1 || btype != 2) {
        HeapFree(heap, 0, out.data);
        huffTreeFree(litlenTree, heap);
        return 0;
    }

    while (1) {
        int sym = DecodeLiteralLength(litlenTree, &in, &bitBuf, &bitCnt);
        if (sym < 0) break;
        if (sym < 256) {
            BYTE b = (BYTE)sym;
            OutStream_Write(&out, b);
            window[windowFill++ % WSIZE] = b;
        } else if (sym == 256) {
            break;
        } else {
            int len = 0, extraBits = 0, extra = 0;
            if (sym <= 264) {
                len = sym - 254;
                extraBits = 0;
            } else if (sym <= 268) {
                len = 11 + ((sym - 265) << 1);
                extraBits = 1;
            } else if (sym <= 272) {
                len = 19 + ((sym - 269) << 2);
                extraBits = 2;
            } else if (sym <= 276) {
                len = 35 + ((sym - 273) << 3);
                extraBits = 3;
            } else if (sym <= 280) {
                len = 67 + ((sym - 277) << 4);
                extraBits = 4;
            } else if (sym <= 284) {
                len = 131 + ((sym - 281) << 5);
                extraBits = 5;
            } else if (sym == 285) {
                len = 258;
                extraBits = 0;
            }
            if (extraBits) {
                extra = (int)ReadBits(&in, extraBits, &bitBuf, &bitCnt);
                len += extra;
            }
            DWORD distCode = ReadBits(&in, 5, &bitBuf, &bitCnt);
            DWORD dist = 0, distExtraBits = 0, distExtra = 0;
            if (distCode <= 3) {
                dist = distCode + 1;
                distExtraBits = 0;
            } else if (distCode <= 5) {
                dist = 5 + ((distCode - 4) << 1);
                distExtraBits = 1;
            } else if (distCode <= 7) {
                dist = 9 + ((distCode - 6) << 2);
                distExtraBits = 2;
            } else if (distCode <= 9) {
                dist = 17 + ((distCode - 8) << 3);
                distExtraBits = 3;
            } else if (distCode <= 11) {
                dist = 33 + ((distCode - 10) << 4);
                distExtraBits = 4;
            } else if (distCode <= 13) {
                dist = 65 + ((distCode - 12) << 5);
                distExtraBits = 5;
            } else if (distCode <= 15) {
                dist = 129 + ((distCode - 14) << 6);
                distExtraBits = 6;
            } else if (distCode <= 17) {
                dist = 257 + ((distCode - 16) << 7);
                distExtraBits = 7;
            } else if (distCode <= 19) {
                dist = 513 + ((distCode - 18) << 8);
                distExtraBits = 8;
            } else if (distCode <= 21) {
                dist = 1025 + ((distCode - 20) << 9);
                distExtraBits = 9;
            } else if (distCode <= 23) {
                dist = 2049 + ((distCode - 22) << 10);
                distExtraBits = 10;
            } else if (distCode <= 25) {
                dist = 4097 + ((distCode - 24) << 11);
                distExtraBits = 11;
            } else if (distCode <= 27) {
                dist = 8193 + ((distCode - 26) << 12);
                distExtraBits = 12;
            } else {
                dist = 16385 + ((distCode - 28) << 13);
                distExtraBits = 13;
            }
            if (distExtraBits) {
                distExtra = (int)ReadBits(&in, distExtraBits, &bitBuf, &bitCnt);
                dist += distExtra;
            }
            for (int i = 0; i < len; i++) {
                BYTE b = window[(windowFill - dist) % WSIZE];
                OutStream_Write(&out, b);
                window[windowFill++ % WSIZE] = b;
            }
        }
    }

    huffTreeFree(litlenTree, heap);
    *output = out.data;
    return out.size;
}

// ---------- Helper functions ----------
static BOOL CreateDirectoryRecursive(LPCWSTR path) {
    if (CreateDirectoryW(path, NULL)) return TRUE;
    if (GetLastError() == ERROR_ALREADY_EXISTS) return TRUE;
    wchar_t parent[MAX_PATH];
    wcscpy_s(parent, MAX_PATH, path);
    wchar_t* last = wcsrchr(parent, L'\\');
    if (last) {
        *last = 0;
        if (!CreateDirectoryRecursive(parent)) return FALSE;
        return CreateDirectoryW(path, NULL) || GetLastError() == ERROR_ALREADY_EXISTS;
    }
    return FALSE;
}

static BYTE* ReadFilePart(HANDLE hFile, DWORD offset, DWORD size, HANDLE heap) {
    BYTE* buf = (BYTE*)HeapAlloc(heap, 0, size);
    if (!buf) return NULL;
    SetFilePointer(hFile, offset, NULL, FILE_BEGIN);
    DWORD read;
    if (!ReadFile(hFile, buf, size, &read, NULL) || read != size) {
        HeapFree(heap, 0, buf);
        return NULL;
    }
    return buf;
}

static void LogToStatus(HWND hwndStatus, const wchar_t* msg) {
    if (hwndStatus) {
        SendMessageW(hwndStatus, EM_SETSEL, (WPARAM)-1, (LPARAM)-1);
        SendMessageW(hwndStatus, EM_REPLACESEL, FALSE, (LPARAM)msg);
    }
}

// ---------- Поиск EOCD ----------
BOOL FindEOCD(HANDLE hFile, DWORD fileSize, EndOfCentralDirectory* out, DWORD* outOffset) {
    const DWORD MAX_COMMENT = 0xFFFF;
    DWORD searchSize = (fileSize > MAX_COMMENT + sizeof(EndOfCentralDirectory))
                        ? MAX_COMMENT + sizeof(EndOfCentralDirectory)
                        : fileSize;
    BYTE* buf = (BYTE*)HeapAlloc(GetProcessHeap(), 0, searchSize);
    if (!buf) return FALSE;

    DWORD start = fileSize - searchSize;
    SetFilePointer(hFile, start, NULL, FILE_BEGIN);
    DWORD read;
    if (!ReadFile(hFile, buf, searchSize, &read, NULL)) {
        HeapFree(GetProcessHeap(), 0, buf);
        return FALSE;
    }

    for (DWORD i = read - sizeof(EndOfCentralDirectory); i > 0; i--) {
        EndOfCentralDirectory* eocd = (EndOfCentralDirectory*)(buf + i);
        if (eocd->signature == 0x06054b50) {
            *out = *eocd;
            *outOffset = start + i;
            HeapFree(GetProcessHeap(), 0, buf);
            return TRUE;
        }
    }

    HeapFree(GetProcessHeap(), 0, buf);
    return FALSE;
}

static BOOL ParseCentralDirectory(BYTE* cdData, DWORD size,
                                   CentralDirectoryFileHeader* entries, int maxEntries,
                                   int* outCount) {
    DWORD pos = 0;
    int count = 0;
    while (pos + sizeof(CentralDirectoryFileHeader) < size && count < maxEntries) {
        CentralDirectoryFileHeader* cd = (CentralDirectoryFileHeader*)(cdData + pos);
        if (cd->signature != 0x02014b50) {
            break;
        }
        entries[count++] = *cd;
        pos += sizeof(CentralDirectoryFileHeader);
        pos += cd->fileNameLength;
        pos += cd->extraFieldLength;
        pos += cd->fileCommentLength;
    }
    *outCount = count;
    return (count > 0);
}

static BOOL ExtractFile(const CentralDirectoryFileHeader* cd,
                        LPCWSTR outDir,
                        HANDLE hZip,
                        DWORD zipStartOffset,
                        BOOL verbose,
                        HANDLE heap,
                        HWND hwndStatus) {
    DWORD localAbsOffset = zipStartOffset + cd->localHeaderOffset;
    LocalFileHeader lh;
    SetFilePointer(hZip, localAbsOffset, NULL, FILE_BEGIN);
    DWORD read;
    if (!ReadFile(hZip, &lh, sizeof(lh), &read, NULL) || read != sizeof(lh)) {
        LogToStatus(hwndStatus, L"Failed to read local header.\r\n");
        return FALSE;
    }

    if (lh.signature != 0x04034b50) {
        wchar_t msg[128];
        wsprintfW(msg, L"Warning: bad local header signature (0x%X), using CD data.\r\n", lh.signature);
        LogToStatus(hwndStatus, msg);
    }

    DWORD nameLen = cd->fileNameLength;
    BYTE* utf8Name = (BYTE*)HeapAlloc(heap, 0, nameLen + 1);
    if (!utf8Name) return FALSE;
    SetFilePointer(hZip, localAbsOffset + sizeof(LocalFileHeader), NULL, FILE_BEGIN);
    if (!ReadFile(hZip, utf8Name, nameLen, &read, NULL) || read != nameLen) {
        HeapFree(heap, 0, utf8Name);
        return FALSE;
    }
    utf8Name[nameLen] = 0;
    int nameLenW = MultiByteToWideChar(CP_UTF8, 0, (char*)utf8Name, -1, NULL, 0);
    wchar_t* fileNameW = (wchar_t*)HeapAlloc(heap, 0, nameLenW * sizeof(wchar_t));
    MultiByteToWideChar(CP_UTF8, 0, (char*)utf8Name, -1, fileNameW, nameLenW);
    HeapFree(heap, 0, utf8Name);

    wchar_t outPath[MAX_PATH];
    wcscpy_s(outPath, MAX_PATH, outDir);
    wcscat_s(outPath, MAX_PATH, L"\\");
    wcscat_s(outPath, MAX_PATH, fileNameW);

    wchar_t* lastSlash = wcsrchr(outPath, L'\\');
    if (lastSlash) {
        *lastSlash = 0;
        CreateDirectoryRecursive(outPath);
        *lastSlash = L'\\';
    }

    DWORD dataOffset = localAbsOffset + sizeof(LocalFileHeader) + lh.fileNameLength + lh.extraFieldLength;
    BYTE* compData = ReadFilePart(hZip, dataOffset, cd->compressedSize, heap);
    if (!compData) {
        HeapFree(heap, 0, fileNameW);
        return FALSE;
    }

    BYTE* outData = NULL;
    DWORD outSize = 0;
    if (cd->compression == 0) {
        outData = compData;
        outSize = cd->uncompressedSize;
    } else if (cd->compression == 8) {
        outSize = InflateData(compData, cd->compressedSize, &outData, heap);
        if (outSize != cd->uncompressedSize) {
            if (outData) HeapFree(heap, 0, outData);
            HeapFree(heap, 0, compData);
            HeapFree(heap, 0, fileNameW);
            LogToStatus(hwndStatus, L"Deflate decompression failed.\r\n");
            return FALSE;
        }
        HeapFree(heap, 0, compData);
        compData = NULL;
    } else {
        HeapFree(heap, 0, compData);
        HeapFree(heap, 0, fileNameW);
        return FALSE;
    }

    HANDLE hOut = CreateFileW(outPath, GENERIC_WRITE, 0, NULL, CREATE_ALWAYS, FILE_ATTRIBUTE_NORMAL, NULL);
    if (hOut == INVALID_HANDLE_VALUE) {
        if (outData && cd->compression == 8) HeapFree(heap, 0, outData);
        if (compData) HeapFree(heap, 0, compData);
        HeapFree(heap, 0, fileNameW);
        return FALSE;
    }
    DWORD written;
    WriteFile(hOut, (cd->compression == 0) ? compData : outData, outSize, &written, NULL);
    CloseHandle(hOut);

    FILETIME ft;
    DosDateTimeToFileTime(cd->modDate, cd->modTime, &ft);
    LocalFileTimeToFileTime(&ft, &ft);
    HANDLE hOut2 = CreateFileW(outPath, FILE_WRITE_ATTRIBUTES, FILE_SHARE_READ, NULL, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, NULL);
    if (hOut2 != INVALID_HANDLE_VALUE) {
        SetFileTime(hOut2, NULL, NULL, &ft);
        CloseHandle(hOut2);
    }

    if (verbose && hwndStatus) {
        wchar_t msg[512];
        wsprintfW(msg, L"Extracted: %s (%u bytes)\r\n", fileNameW, outSize);
        LogToStatus(hwndStatus, msg);
    }

    HeapFree(heap, 0, fileNameW);
    if (cd->compression == 8) HeapFree(heap, 0, outData);
    if (compData) HeapFree(heap, 0, compData);
    return TRUE;
}

// ---------- Главная функция извлечения из SFX ----------
BOOL ExtractFromSFX(LPCWSTR exePath, LPCWSTR destDir, BOOL verbose, HWND hwndStatus) {
    HANDLE heap = GetProcessHeap();
    HANDLE hFile = CreateFileW(exePath, GENERIC_READ, FILE_SHARE_READ, NULL, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, NULL);
    if (hFile == INVALID_HANDLE_VALUE) {
        LogToStatus(hwndStatus, L"Failed to open EXE file.\r\n");
        return FALSE;
    }

    DWORD fileSize = GetFileSize(hFile, NULL);
    if (fileSize == INVALID_FILE_SIZE) {
        CloseHandle(hFile);
        return FALSE;
    }

    EndOfCentralDirectory eocd;
    DWORD eocdOffset;
    if (!FindEOCD(hFile, fileSize, &eocd, &eocdOffset)) {
        LogToStatus(hwndStatus, L"End of Central Directory not found.\r\n");
        CloseHandle(hFile);
        return FALSE;
    }

    DWORD zipStart = eocdOffset - eocd.centralDirSize - eocd.centralDirOffset;
    if (zipStart >= fileSize) {
        LogToStatus(hwndStatus, L"Invalid ZIP start offset.\r\n");
        CloseHandle(hFile);
        return FALSE;
    }

    wchar_t msg[256];
    wsprintfW(msg, L"ZIP start at 0x%X (%u), entries: %u\r\n", zipStart, zipStart, eocd.totalEntries);
    LogToStatus(hwndStatus, msg);

    DWORD cdOffset = zipStart + eocd.centralDirOffset;
    BYTE* cdData = (BYTE*)HeapAlloc(heap, 0, eocd.centralDirSize);
    if (!cdData) {
        CloseHandle(hFile);
        return FALSE;
    }
    SetFilePointer(hFile, cdOffset, NULL, FILE_BEGIN);
    DWORD bytesRead;
    if (!ReadFile(hFile, cdData, eocd.centralDirSize, &bytesRead, NULL) || bytesRead != eocd.centralDirSize) {
        LogToStatus(hwndStatus, L"Failed to read central directory.\r\n");
        HeapFree(heap, 0, cdData);
        CloseHandle(hFile);
        return FALSE;
    }

    CentralDirectoryFileHeader entries[1024];
    int entryCount = 0;
    if (!ParseCentralDirectory(cdData, eocd.centralDirSize, entries, 1024, &entryCount) || entryCount == 0) {
        LogToStatus(hwndStatus, L"Central directory is empty or corrupted.\r\n");
        HeapFree(heap, 0, cdData);
        CloseHandle(hFile);
        return FALSE;
    }

    int extracted = 0;
    for (int i = 0; i < entryCount; i++) {
        if (ExtractFile(&entries[i], destDir, hFile, zipStart, verbose, heap, hwndStatus)) {
            extracted++;
        } else {
            wsprintfW(msg, L"Failed to extract entry #%d\r\n", i+1);
            LogToStatus(hwndStatus, msg);
        }
    }

    HeapFree(heap, 0, cdData);
    CloseHandle(hFile);
    wsprintfW(msg, L"\r\nExtracted %d of %d files.\r\n", extracted, entryCount);
    LogToStatus(hwndStatus, msg);
    return (extracted == entryCount);
}

// ---------- Чтение ZIP-комментария ----------
BOOL GetZipComment(LPCWSTR exePath, wchar_t** commentOut, HANDLE heap) {
    HANDLE hFile = CreateFileW(exePath, GENERIC_READ, FILE_SHARE_READ, NULL, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, NULL);
    if (hFile == INVALID_HANDLE_VALUE) return FALSE;

    DWORD fileSize = GetFileSize(hFile, NULL);
    if (fileSize == INVALID_FILE_SIZE) {
        CloseHandle(hFile);
        return FALSE;
    }

    EndOfCentralDirectory eocd;
    DWORD eocdOffset;
    if (!FindEOCD(hFile, fileSize, &eocd, &eocdOffset)) {
        CloseHandle(hFile);
        return FALSE;
    }

    if (eocd.commentLength == 0) {
        CloseHandle(hFile);
        return FALSE;
    }

    DWORD commentOffset = eocdOffset + sizeof(EndOfCentralDirectory);
    BYTE* commentBytes = (BYTE*)HeapAlloc(heap, 0, eocd.commentLength + 1);
    if (!commentBytes) {
        CloseHandle(hFile);
        return FALSE;
    }

    SetFilePointer(hFile, commentOffset, NULL, FILE_BEGIN);
    DWORD bytesRead;
    if (!ReadFile(hFile, commentBytes, eocd.commentLength, &bytesRead, NULL) || bytesRead != eocd.commentLength) {
        HeapFree(heap, 0, commentBytes);
        CloseHandle(hFile);
        return FALSE;
    }
    commentBytes[eocd.commentLength] = 0;

    int wlen = MultiByteToWideChar(CP_UTF8, 0, (char*)commentBytes, eocd.commentLength, NULL, 0);
    if (wlen == 0) {
        wlen = MultiByteToWideChar(CP_ACP, 0, (char*)commentBytes, eocd.commentLength, NULL, 0);
    }
    if (wlen == 0) {
        HeapFree(heap, 0, commentBytes);
        CloseHandle(hFile);
        return FALSE;
    }

    wchar_t* commentW = (wchar_t*)HeapAlloc(heap, 0, (wlen + 1) * sizeof(wchar_t));
    if (!commentW) {
        HeapFree(heap, 0, commentBytes);
        CloseHandle(hFile);
        return FALSE;
    }

    if (MultiByteToWideChar(CP_UTF8, 0, (char*)commentBytes, eocd.commentLength, commentW, wlen) == 0) {
        MultiByteToWideChar(CP_ACP, 0, (char*)commentBytes, eocd.commentLength, commentW, wlen);
    }
    commentW[wlen] = 0;

    HeapFree(heap, 0, commentBytes);
    CloseHandle(hFile);
    *commentOut = commentW;
    return TRUE;
}