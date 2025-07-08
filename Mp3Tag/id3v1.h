#ifndef ID3V1_H
#define ID3V1_H

#include <stdio.h>

// ID3v1 tag structure
typedef struct {
    char id[3];
    char title[30];
    char artist[30];
    char album[30];
    char year[4];
    char comment[30];
    unsigned char track;
    unsigned char genre;
} ID3v1Tag;

#define ID3V1_COMMENT_SIZE 30
#define ID3V1_TRACK_POS 126
#define ID3V1_GENRE_POS 127

void print_id3v1_tags(FILE *f);

#endif // ID3V1_H