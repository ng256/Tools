#ifndef ID3V2_H
#define ID3V2_H

#include <stdio.h>

// ID3v2 header structure
typedef struct {
    char id[3];       // "ID3"
    unsigned char ver;
    unsigned char rev;
    unsigned char flags;
    unsigned char size[4]; // syncsafe tag size
} ID3v2Header;

// Frame header structure
typedef struct {
    char id[4];
    unsigned char size[4];
    unsigned char flags[2];
} ID3v2FrameHeader;

// Encoding types
#define ENCODING_ISO8859 0
#define ENCODING_UTF16 1
#define ENCODING_UTF16BE 2
#define ENCODING_UTF8 3

// Frame identifiers
#define FRAME_COMMENT "COMM"
#define FRAME_GENRE "TCON"

int syncsafe_to_size(const unsigned char size[4]);
void print_id3v2_tags(FILE *f);

#endif // ID3V2_H