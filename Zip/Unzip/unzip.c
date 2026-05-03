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

// ---------- Inflate (fixed Huffman) ----------
#define WSIZE 32768
#define MAX_BITS 9

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

static void OutStream_WriteBlock(OutStream* out, const BYTE* src, DWORD len) {
    for (DWORD i = 0; i < len; i++)
        OutStream_Write(out, src[i]);
}

// Build fixed Huffman tree for literal/length (0-287)
static HuffNode* BuildFixedTree(HANDLE heap) {
    // Code lengths (RFC 1951)
    BYTE lengths[288];
    int i;
    for (i = 0; i < 144; i++) lengths[i] = 8;
    for (i = 144; i < 256; i++) lengths[i] = 9;
    for (i = 256; i < 280; i++) lengths[i] = 7;
    for (i = 280; i < 288; i++) lengths[i] = 8;

    // Generate canonical codes
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
        // Insert rev (bits from MSB to LSB)
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

// Decode a literal/length symbol
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

    // Read BFINAL and BTYPE (we expect final block, fixed Huffman)
    DWORD bfinal = ReadBits(&in, 1, &bitBuf, &bitCnt);
    DWORD btype = ReadBits(&in, 2, &bitBuf, &bitCnt);
    if (bfinal != 1 || btype != 2) {
        // Not a valid block from our simple compressor – fallback: treat as uncompressed
        // For robustness, just copy compressed data as output (store)
        HeapFree(heap, 0, out.data);
        huffTreeFree(litlenTree, heap);
        // Not implemented fallback, but we return zero
        return 0;
    }

    while (1) {
        int sym = DecodeLiteralLength(litlenTree, &in, &bitBuf, &bitCnt);
        if (sym < 0) break;
        if (sym < 256) { // literal
            BYTE b = (BYTE)sym;
            OutStream_Write(&out, b);
            window[windowFill++ % WSIZE] = b;
        } else if (sym == 256) {
            break; // end of block
        } else { // length symbol
            // Get length base and extra bits
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
            // Read distance (5 bits fixed)
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
            // Copy from window
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

// ---------- ZIP helpers ----------
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

static BOOL ExtractFile(const CentralDirectoryFileHeader* cd, LPCWSTR outDir, HANDLE hZip, BOOL verbose, HANDLE heap) {
    // Read local header
    LocalFileHeader lh;
    SetFilePointer(hZip, cd->localHeaderOffset, NULL, FILE_BEGIN);
    DWORD read;
    if (!ReadFile(hZip, &lh, sizeof(lh), &read, NULL) || read != sizeof(lh) || lh.signature != 0x04034b50)
        return FALSE;

    // Read file name
    BYTE* utf8Name = (BYTE*)HeapAlloc(heap, 0, lh.fileNameLength + 1);
    if (!utf8Name) return FALSE;
    if (!ReadFile(hZip, utf8Name, lh.fileNameLength, &read, NULL) || read != lh.fileNameLength) {
        HeapFree(heap, 0, utf8Name);
        return FALSE;
    }
    utf8Name[lh.fileNameLength] = 0;
    int nameLenW = MultiByteToWideChar(CP_UTF8, 0, (char*)utf8Name, -1, NULL, 0);
    wchar_t* fileNameW = (wchar_t*)HeapAlloc(heap, 0, nameLenW * sizeof(wchar_t));
    MultiByteToWideChar(CP_UTF8, 0, (char*)utf8Name, -1, fileNameW, nameLenW);
    HeapFree(heap, 0, utf8Name);

    // Build output path
    wchar_t outPath[MAX_PATH];
    wcscpy_s(outPath, MAX_PATH, outDir);
    wcscat_s(outPath, MAX_PATH, L"\\");
    wcscat_s(outPath, MAX_PATH, fileNameW);
    // Create subdirectories if needed
    wchar_t* lastSlash = wcsrchr(outPath, L'\\');
    if (lastSlash) {
        *lastSlash = 0;
        CreateDirectoryRecursive(outPath);
        *lastSlash = L'\\';
    }

    // Read compressed data
    DWORD dataOffset = cd->localHeaderOffset + sizeof(LocalFileHeader) + lh.fileNameLength + lh.extraFieldLength;
    BYTE* compData = ReadFilePart(hZip, dataOffset, cd->compressedSize, heap);
    if (!compData) {
        HeapFree(heap, 0, fileNameW);
        return FALSE;
    }

    BYTE* outData = NULL;
    DWORD outSize = 0;
    if (cd->compression == 0) { // Store
        outData = compData;
        outSize = cd->uncompressedSize;
        // compData will be freed later, but output uses same buffer – we must duplicate or not free yet.
        // For store, we will write directly from compData, then free.
    } else if (cd->compression == 8) { // Deflate
        outSize = InflateData(compData, cd->compressedSize, &outData, heap);
        if (outSize != cd->uncompressedSize) {
            if (outData) HeapFree(heap, 0, outData);
            HeapFree(heap, 0, compData);
            HeapFree(heap, 0, fileNameW);
            return FALSE;
        }
        HeapFree(heap, 0, compData);
        compData = NULL;
    } else {
        HeapFree(heap, 0, compData);
        HeapFree(heap, 0, fileNameW);
        return FALSE;
    }

    // Write output file
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

    // Set file time from DOS date/time
    SYSTEMTIME st;
    FILETIME ft;
    DosDateTimeToFileTime(cd->modDate, cd->modTime, &ft);
    LocalFileTimeToFileTime(&ft, &ft);
    HANDLE hOut2 = CreateFileW(outPath, FILE_WRITE_ATTRIBUTES, FILE_SHARE_READ, NULL, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, NULL);
    if (hOut2 != INVALID_HANDLE_VALUE) {
        SetFileTime(hOut2, NULL, NULL, &ft);
        CloseHandle(hOut2);
    }

    if (verbose) {
        wchar_t msg[512];
        wsprintfW(msg, L"Extracted: %s (%u bytes)", fileNameW, outSize);
        HANDLE hOutMsg = GetStdHandle(STD_OUTPUT_HANDLE);
        DWORD writtenMsg;
        WriteFile(hOutMsg, msg, wcslen(msg) * sizeof(wchar_t), &writtenMsg, NULL);
        WriteFile(hOutMsg, L"\n", 2, &writtenMsg, NULL);
    }

    HeapFree(heap, 0, fileNameW);
    if (cd->compression == 8) HeapFree(heap, 0, outData);
    if (compData) HeapFree(heap, 0, compData);
    return TRUE;
}

BOOL ExtractZipFile(LPCWSTR zipFileName, LPCWSTR outputDir, BOOL verbose) {
    HANDLE heap = GetProcessHeap();
    HANDLE hZip = CreateFileW(zipFileName, GENERIC_READ, FILE_SHARE_READ, NULL, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, NULL);
    if (hZip == INVALID_HANDLE_VALUE) return FALSE;

    DWORD fileSize = GetFileSize(hZip, NULL);
    if (fileSize == INVALID_FILE_SIZE) { CloseHandle(hZip); return FALSE; }

    // Locate End of Central Directory
    EndOfCentralDirectory eocd;
    BOOL found = FALSE;
    for (DWORD offset = (fileSize > 65536 ? fileSize - 65536 : 0); offset < fileSize - sizeof(eocd); offset++) {
        SetFilePointer(hZip, offset, NULL, FILE_BEGIN);
        DWORD read;
        ReadFile(hZip, &eocd, sizeof(eocd), &read, NULL);
        if (read == sizeof(eocd) && eocd.signature == 0x06054b50) {
            found = TRUE;
            break;
        }
    }
    if (!found) { CloseHandle(hZip); return FALSE; }

    // Read central directory
    DWORD cdSize = eocd.centralDirSize;
    DWORD cdOffset = eocd.centralDirOffset;
    BYTE* cdData = ReadFilePart(hZip, cdOffset, cdSize, heap);
    if (!cdData) { CloseHandle(hZip); return FALSE; }

    // Parse central directory entries
    DWORD pos = 0;
    for (int i = 0; i < eocd.totalEntries; i++) {
        CentralDirectoryFileHeader* cd = (CentralDirectoryFileHeader*)(cdData + pos);
        if (cd->signature != 0x02014b50) break;
        // Skip variable fields
        DWORD entrySize = sizeof(CentralDirectoryFileHeader) + cd->fileNameLength + cd->extraFieldLength + cd->fileCommentLength;
        if (!ExtractFile(cd, outputDir, hZip, verbose, heap)) {
            // Continue on error? We'll break.
            break;
        }
        pos += entrySize;
    }

    HeapFree(heap, 0, cdData);
    CloseHandle(hZip);
    return TRUE;
}
