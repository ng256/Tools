#include "zip.h"
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

// ------------------------------------------------------------
// CRC32 (software table)
static DWORD crc32_table[256];
static int crc32_init = 0;

static void InitCRC32Table(void) {
    if (crc32_init) return;
    const DWORD poly = 0xEDB88320;
    for (DWORD i = 0; i < 256; i++) {
        DWORD crc = i;
        for (int j = 0; j < 8; j++)
            crc = (crc & 1) ? (crc >> 1) ^ poly : crc >> 1;
        crc32_table[i] = crc;
    }
    crc32_init = 1;
}

static DWORD UpdateCRC32(DWORD crc, const BYTE* data, DWORD len) {
    InitCRC32Table();
    crc ^= 0xFFFFFFFF;
    for (DWORD i = 0; i < len; i++)
        crc = crc32_table[(crc ^ data[i]) & 0xFF] ^ (crc >> 8);
    return crc ^ 0xFFFFFFFF;
}

// ------------------------------------------------------------
// Deflate (Fixed Huffman, LSB-first, correct termination)
#define WSIZE 32768
#define MIN_MATCH 3
#define MAX_MATCH 258

typedef struct {
    BYTE window[WSIZE];
    DWORD window_fill;
} LZ77;

static void LZ77_Init(LZ77* lz) {
    memset(lz->window, 0, WSIZE);
    lz->window_fill = 0;
}

static void LZ77_Add(LZ77* lz, const BYTE* data, DWORD len) {
    DWORD toCopy = len;
    if (toCopy > WSIZE - lz->window_fill)
        toCopy = WSIZE - lz->window_fill;
    memcpy(lz->window + lz->window_fill, data, toCopy);
    lz->window_fill += toCopy;
}

static void LZ77_FindMatch(LZ77* lz, const BYTE* lookahead, DWORD lookaheadSize, WORD* dist, WORD* len) {
    *dist = 0;
    *len = 0;
    if (lookaheadSize < MIN_MATCH || lz->window_fill < MIN_MATCH) return;
    DWORD maxOffset = lz->window_fill;
    if (maxOffset > WSIZE) maxOffset = WSIZE;
    WORD bestDist = 0, bestLen = 0;
    for (DWORD offset = 1; offset <= maxOffset; offset++) {
        const BYTE* ptr = lz->window + lz->window_fill - offset;
        if (*ptr != lookahead[0]) continue;
        WORD matchLen = 1;
        while (matchLen < lookaheadSize && matchLen < MAX_MATCH &&
               (ptr + matchLen) < (lz->window + lz->window_fill) &&
               ptr[matchLen] == lookahead[matchLen]) {
            matchLen++;
        }
        if (matchLen >= MIN_MATCH && matchLen > bestLen) {
            bestLen = matchLen;
            bestDist = (WORD)offset;
            if (bestLen == MAX_MATCH) break;
        }
    }
    *dist = bestDist;
    *len = bestLen;
}

typedef struct {
    BYTE* data;
    DWORD capacity;
    DWORD pos;
    HANDLE heap;
} BitStream;

static void BS_Init(BitStream* bs, HANDLE heap) {
    bs->capacity = 4096;
    bs->data = (BYTE*)HeapAlloc(heap, HEAP_ZERO_MEMORY, bs->capacity);
    bs->pos = 0;
    bs->heap = heap;
}

static void BS_WriteBit(BitStream* bs, int bit) {
    DWORD bytePos = bs->pos >> 3;
    DWORD bitPos = bs->pos & 7;
    if (bytePos >= bs->capacity) {
        DWORD newCap = bs->capacity * 2;
        BYTE* newData = (BYTE*)HeapReAlloc(bs->heap, HEAP_ZERO_MEMORY, bs->data, newCap);
        if (!newData) return;
        bs->data = newData;
        bs->capacity = newCap;
    }
    if (bit)
        bs->data[bytePos] |= (1 << bitPos);
    else
        bs->data[bytePos] &= ~(1 << bitPos);
    bs->pos++;
}

static void BS_WriteBits(BitStream* bs, DWORD code, int bits) {
    for (int i = 0; i < bits; i++)
        BS_WriteBit(bs, (code >> i) & 1);
}

static void BS_Flush(BitStream* bs) {
    while (bs->pos & 7)
        BS_WriteBit(bs, 0);
}

static WORD ReverseBits(WORD code, int bits) {
    WORD result = 0;
    for (int i = 0; i < bits; i++) {
        result = (result << 1) | (code & 1);
        code >>= 1;
    }
    return result;
}

// Fixed Huffman codes (RFC 1951)
static WORD litlen_code[288];
static BYTE litlen_bits[288];
static int fixed_init = 0;

static void InitFixedCodes(void) {
    if (fixed_init) return;
    int i;
    for (i = 0; i < 144; i++) { litlen_code[i] = 0x30 + i; litlen_bits[i] = 8; }
    for (i = 144; i < 256; i++) { litlen_code[i] = 0x190 + (i - 144); litlen_bits[i] = 9; }
    for (i = 256; i < 280; i++) { litlen_code[i] = i - 256; litlen_bits[i] = 7; }
    for (i = 280; i < 288; i++) { litlen_code[i] = 0xC0 + (i - 280); litlen_bits[i] = 8; }
    fixed_init = 1;
}

static void EncodeLength(WORD len, WORD* code, WORD* extra, WORD* extraBits) {
    if (len <= 10) {
        *code = 254 + len;
        *extraBits = 0; *extra = 0;
    } else if (len <= 18) {
        *code = 265 + ((len - 11) >> 1);
        *extraBits = 1; *extra = (len - 11) & 1;
    } else if (len <= 34) {
        *code = 269 + ((len - 19) >> 2);
        *extraBits = 2; *extra = (len - 19) & 3;
    } else if (len <= 66) {
        *code = 273 + ((len - 35) >> 3);
        *extraBits = 3; *extra = (len - 35) & 7;
    } else if (len <= 130) {
        *code = 277 + ((len - 67) >> 4);
        *extraBits = 4; *extra = (len - 67) & 15;
    } else if (len <= 258) {
        *code = 281 + ((len - 131) >> 5);
        *extraBits = 5; *extra = (len - 131) & 31;
    } else {
        *code = 285; *extraBits = 0; *extra = 0;
    }
}

static void EncodeDistance(WORD dist, WORD* code, WORD* extra, WORD* extraBits) {
    if (dist <= 4) {
        *code = dist - 1; *extraBits = 0; *extra = 0;
    } else if (dist <= 8) {
        *code = 4 + ((dist - 5) >> 1); *extraBits = 1; *extra = (dist - 5) & 1;
    } else if (dist <= 16) {
        *code = 6 + ((dist - 9) >> 2); *extraBits = 2; *extra = (dist - 9) & 3;
    } else if (dist <= 32) {
        *code = 8 + ((dist - 17) >> 3); *extraBits = 3; *extra = (dist - 17) & 7;
    } else if (dist <= 64) {
        *code = 10 + ((dist - 33) >> 4); *extraBits = 4; *extra = (dist - 33) & 15;
    } else if (dist <= 128) {
        *code = 12 + ((dist - 65) >> 5); *extraBits = 5; *extra = (dist - 65) & 31;
    } else if (dist <= 256) {
        *code = 14 + ((dist - 129) >> 6); *extraBits = 6; *extra = (dist - 129) & 63;
    } else if (dist <= 512) {
        *code = 16 + ((dist - 257) >> 7); *extraBits = 7; *extra = (dist - 257) & 127;
    } else if (dist <= 1024) {
        *code = 18 + ((dist - 513) >> 8); *extraBits = 8; *extra = (dist - 513) & 255;
    } else if (dist <= 2048) {
        *code = 20 + ((dist - 1025) >> 9); *extraBits = 9; *extra = (dist - 1025) & 511;
    } else if (dist <= 4096) {
        *code = 22 + ((dist - 2049) >> 10); *extraBits = 10; *extra = (dist - 2049) & 1023;
    } else if (dist <= 8192) {
        *code = 24 + ((dist - 4097) >> 11); *extraBits = 11; *extra = (dist - 4097) & 2047;
    } else if (dist <= 16384) {
        *code = 26 + ((dist - 8193) >> 12); *extraBits = 12; *extra = (dist - 8193) & 4095;
    } else {
        *code = 28 + ((dist - 16385) >> 13); *extraBits = 13; *extra = (dist - 16385) & 8191;
    }
}

static void EmitLiteral(BitStream* bs, BYTE lit) {
    BS_WriteBits(bs, ReverseBits(litlen_code[lit], litlen_bits[lit]), litlen_bits[lit]);
}

static void EmitLengthDistance(BitStream* bs, WORD len, WORD dist) {
    WORD code, extra, extraBits;
    EncodeLength(len, &code, &extra, &extraBits);
    BS_WriteBits(bs, ReverseBits(litlen_code[code], litlen_bits[code]), litlen_bits[code]);
    if (extraBits) BS_WriteBits(bs, extra, extraBits);
    EncodeDistance(dist, &code, &extra, &extraBits);
    BS_WriteBits(bs, ReverseBits((WORD)code, 5), 5);
    if (extraBits) BS_WriteBits(bs, extra, extraBits);
}

static void EmitEndOfBlock(BitStream* bs) {
    // Code 256 = end of block
    BS_WriteBits(bs, ReverseBits(litlen_code[256], litlen_bits[256]), litlen_bits[256]);
}

static void DeflateBlock(BitStream* bs, const BYTE* input, DWORD size, BOOL last) {
    InitFixedCodes();
    BS_WriteBits(bs, last ? 1 : 0, 1);
    BS_WriteBits(bs, 2, 2); // fixed Huffman

    LZ77 lz;
    LZ77_Init(&lz);
    DWORD i = 0;
    while (i < size) {
        WORD dist, len;
        LZ77_FindMatch(&lz, input + i, size - i, &dist, &len);
        if (len >= MIN_MATCH) {
            EmitLengthDistance(bs, len, dist);
            LZ77_Add(&lz, input + i, len);
            i += len;
        } else {
            EmitLiteral(bs, input[i]);
            LZ77_Add(&lz, input + i, 1);
            i++;
        }
    }
    EmitEndOfBlock(bs);
}

static DWORD DeflateData(BYTE* input, DWORD inputSize, BYTE** output, HANDLE heap) {
    if (!input || inputSize == 0) return 0;
    BitStream bs;
    BS_Init(&bs, heap);
    DeflateBlock(&bs, input, inputSize, 1);
    BS_Flush(&bs);
    *output = bs.data;
    return (bs.pos + 7) >> 3;
}

// ------------------------------------------------------------
// Helper functions
static LPCWSTR BaseName(LPCWSTR path) {
    LPCWSTR p = path, last = path;
    while (*p) {
        if (*p == L'\\' || *p == L'/') last = p + 1;
        ++p;
    }
    return last;
}

static BYTE* ReadEntireFile(LPCWSTR filename, DWORD* outSize, HANDLE heap) {
    HANDLE hFile = CreateFileW(filename, GENERIC_READ, FILE_SHARE_READ, NULL,
                               OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, NULL);
    if (hFile == INVALID_HANDLE_VALUE) return NULL;
    DWORD size = GetFileSize(hFile, NULL);
    if (size == INVALID_FILE_SIZE) { CloseHandle(hFile); return NULL; }
    BYTE* buf = (BYTE*)HeapAlloc(heap, HEAP_ZERO_MEMORY, size);
    if (!buf) { CloseHandle(hFile); return NULL; }
    DWORD bytesRead;
    BOOL ok = ReadFile(hFile, buf, size, &bytesRead, NULL);
    CloseHandle(hFile);
    if (!ok || bytesRead != size) { HeapFree(heap, 0, buf); return NULL; }
    *outSize = size;
    return buf;
}

static BOOL WriteStruct(HANDLE hFile, const void* data, DWORD size) {
    DWORD written;
    return WriteFile(hFile, data, size, &written, NULL) && written == size;
}

static BYTE* WideToUTF8(LPCWSTR wstr, HANDLE heap, DWORD* outLen) {
    int len = WideCharToMultiByte(CP_UTF8, 0, wstr, -1, NULL, 0, NULL, NULL);
    if (len <= 0) return NULL;
    BYTE* utf8 = (BYTE*)HeapAlloc(heap, 0, len);
    if (!utf8) return NULL;
    WideCharToMultiByte(CP_UTF8, 0, wstr, -1, (char*)utf8, len, NULL, NULL);
    *outLen = len - 1;
    return utf8;
}

// ------------------------------------------------------------
// Main ZIP creation function (structure as in working code)
BOOL CreateZipFile(LPCWSTR zipFileName, LPCWSTR* inputFiles, int fileCount,
                   LPCWSTR globalCommentFile, WORD compressionMethod) {
    HANDLE heap = GetProcessHeap();
    HANDLE hZip = CreateFileW(zipFileName, GENERIC_WRITE, 0, NULL,
                              CREATE_ALWAYS, FILE_ATTRIBUTE_NORMAL, NULL);
    if (hZip == INVALID_HANDLE_VALUE) return FALSE;

    BYTE* globalCommentData = NULL;
    DWORD globalCommentLen = 0;
    if (globalCommentFile)
        globalCommentData = ReadEntireFile(globalCommentFile, &globalCommentLen, heap);

    CentralDirectoryFileHeader* cdHeaders = NULL;
    DWORD cdCount = 0;
    DWORD localHeadersOffset = 0;

    for (int i = 0; i < fileCount; ++i) {
        LPCWSTR filePath = inputFiles[i];
        LPCWSTR fileNameInZip = BaseName(filePath);

        DWORD fileSize;
        BYTE* fileData = ReadEntireFile(filePath, &fileSize, heap);
        if (!fileData) goto error;

        DWORD crc = UpdateCRC32(0, fileData, fileSize);

        HANDLE hSrc = CreateFileW(filePath, GENERIC_READ, FILE_SHARE_READ, NULL,
                                  OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, NULL);
        FILETIME ftLastWrite;
        GetFileTime(hSrc, NULL, NULL, &ftLastWrite);
        CloseHandle(hSrc);
        WORD dosTime = 0, dosDate = 0;
        if (!FileTimeToDosDateTime(&ftLastWrite, &dosDate, &dosTime)) {
            SYSTEMTIME st;
            GetSystemTime(&st);
            dosTime = (WORD)((st.wHour << 11) | (st.wMinute << 5) | (st.wSecond >> 1));
            dosDate = (WORD)(((st.wYear - 1980) << 9) | (st.wMonth << 5) | st.wDay);
        }

        DWORD utf8NameLen;
        BYTE* utf8Name = WideToUTF8(fileNameInZip, heap, &utf8NameLen);
        if (!utf8Name) { HeapFree(heap, 0, fileData); goto error; }

        // Compression (local variable method)
        BYTE* compressedData = NULL;
        DWORD compressedSize = 0;
        WORD method = compressionMethod;
        if (method == ZIP_METHOD_DEFLATE) {
            compressedSize = DeflateData(fileData, fileSize, &compressedData, heap);
            if (compressedSize == 0 || compressedSize >= fileSize) {
                if (compressedData) HeapFree(heap, 0, compressedData);
                compressedData = NULL;
                method = ZIP_METHOD_STORE;
            }
        }

        DWORD finalCompressedSize = (method == ZIP_METHOD_DEFLATE) ? compressedSize : fileSize;
        const BYTE* finalData = (method == ZIP_METHOD_DEFLATE) ? compressedData : fileData;

        LocalFileHeader localHdr;
        localHdr.signature        = 0x04034b50;
        localHdr.versionNeeded    = 20;
        localHdr.flags            = 0;
        localHdr.compression      = method;
        localHdr.modTime          = dosTime;
        localHdr.modDate          = dosDate;
        localHdr.crc32            = crc;
        localHdr.compressedSize   = finalCompressedSize;
        localHdr.uncompressedSize = fileSize;
        localHdr.fileNameLength   = utf8NameLen;
        localHdr.extraFieldLength = 0;

        if (!WriteStruct(hZip, &localHdr, sizeof(localHdr)) ||
            !WriteStruct(hZip, utf8Name, utf8NameLen) ||
            !WriteStruct(hZip, finalData, finalCompressedSize)) {
            if (compressedData) HeapFree(heap, 0, compressedData);
            HeapFree(heap, 0, utf8Name);
            HeapFree(heap, 0, fileData);
            goto error;
        }

        DWORD curOffset = localHeadersOffset;

        CentralDirectoryFileHeader cdHdr;
        cdHdr.signature           = 0x02014b50;
        cdHdr.versionMadeBy       = 20;
        cdHdr.versionNeeded       = 20;
        cdHdr.flags               = 0;
        cdHdr.compression         = method;
        cdHdr.modTime             = dosTime;
        cdHdr.modDate             = dosDate;
        cdHdr.crc32               = crc;
        cdHdr.compressedSize      = finalCompressedSize;
        cdHdr.uncompressedSize    = fileSize;
        cdHdr.fileNameLength      = utf8NameLen;
        cdHdr.extraFieldLength    = 0;
        cdHdr.fileCommentLength   = 0;
        cdHdr.diskNumberStart     = 0;
        cdHdr.internalAttributes  = 0;
        cdHdr.externalAttributes  = 0;
        cdHdr.localHeaderOffset   = curOffset;

        CentralDirectoryFileHeader* newArray = (CentralDirectoryFileHeader*)HeapAlloc(heap, 0, (cdCount + 1) * sizeof(CentralDirectoryFileHeader));
        if (!newArray) {
            if (compressedData) HeapFree(heap, 0, compressedData);
            HeapFree(heap, 0, utf8Name);
            HeapFree(heap, 0, fileData);
            goto error;
        }
        if (cdHeaders) {
            RtlMoveMemory(newArray, cdHeaders, cdCount * sizeof(CentralDirectoryFileHeader));
            HeapFree(heap, 0, cdHeaders);
        }
        newArray[cdCount] = cdHdr;
        cdHeaders = newArray;
        cdCount++;

        if (compressedData) HeapFree(heap, 0, compressedData);
        HeapFree(heap, 0, utf8Name);
        HeapFree(heap, 0, fileData);
        localHeadersOffset += sizeof(LocalFileHeader) + utf8NameLen + finalCompressedSize;
    }

    DWORD centralStartOffset = localHeadersOffset;
    for (int i = 0; i < fileCount; ++i) {
        LPCWSTR fileNameInZip = BaseName(inputFiles[i]);
        DWORD utf8NameLen;
        BYTE* utf8Name = WideToUTF8(fileNameInZip, heap, &utf8NameLen);
        if (!utf8Name) goto error;
        CentralDirectoryFileHeader cdHdr = cdHeaders[i];
        cdHdr.fileNameLength = utf8NameLen;
        if (!WriteStruct(hZip, &cdHdr, sizeof(cdHdr)) ||
            !WriteStruct(hZip, utf8Name, utf8NameLen)) {
            HeapFree(heap, 0, utf8Name);
            goto error;
        }
        HeapFree(heap, 0, utf8Name);
    }

    DWORD centralDirSize = 0;
    for (int i = 0; i < fileCount; ++i) {
        DWORD nameLen;
        BYTE* utf8Name = WideToUTF8(BaseName(inputFiles[i]), heap, &nameLen);
        if (utf8Name) {
            centralDirSize += sizeof(CentralDirectoryFileHeader) + nameLen;
            HeapFree(heap, 0, utf8Name);
        }
    }

    EndOfCentralDirectory end;
    end.signature                 = 0x06054b50;
    end.diskNumber                = 0;
    end.centralDirDiskStart       = 0;
    end.entriesOnThisDisk         = (WORD)fileCount;
    end.totalEntries              = (WORD)fileCount;
    end.centralDirSize            = centralDirSize;
    end.centralDirOffset          = centralStartOffset;
    end.commentLength             = (WORD)globalCommentLen;

    if (!WriteStruct(hZip, &end, sizeof(end))) goto error;
    if (globalCommentLen && globalCommentData) {
        DWORD written;
        WriteFile(hZip, globalCommentData, globalCommentLen, &written, NULL);
    }

    CloseHandle(hZip);
    if (globalCommentData) HeapFree(heap, 0, globalCommentData);
    if (cdHeaders) HeapFree(heap, 0, cdHeaders);
    return TRUE;

error:
    if (hZip != INVALID_HANDLE_VALUE) CloseHandle(hZip);
    if (globalCommentData) HeapFree(heap, 0, globalCommentData);
    if (cdHeaders) HeapFree(heap, 0, cdHeaders);
    return FALSE;
}
