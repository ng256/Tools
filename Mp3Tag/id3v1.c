#include "id3v1.h"
#include "utils.h"
#include <stdio.h>
#include <string.h>

void print_id3v1_tags(FILE *f) {
    if (fseek(f, -ID3V1_TAG_SIZE, SEEK_END) != 0) {
        printf("Error setting file position for ID3v1\n");
        return;
    }
    
    ID3v1Tag tag;
    if (fread(&tag, 1, sizeof(ID3v1Tag), f) != sizeof(ID3v1Tag)) {
        printf("Error reading ID3v1 tag\n");
        return;
    }
    
    if (strncmp(tag.id, "TAG", 3) != 0) {
        printf("ID3v1 tag not found\n");
        return;
    }
    
    printf("ID3v1 tag found:\n");
    printf("Title: %.30s\n", tag.title);
    printf("Artist: %.30s\n", tag.artist);
    printf("Album: %.30s\n", tag.album);
    printf("Year: %.4s\n", tag.year);
    
    // ID3v1.1 detection: last 2 bytes of comment
    if ((unsigned char)tag.comment[28] == 0 && 
        (unsigned char)tag.comment[29] != 0) {
        printf("Comment: %.28s\n", tag.comment);
        printf("Track: %d\n", (unsigned char)tag.comment[29]);
    } else {
        printf("Comment: %.30s\n", tag.comment);
    }
    
    printf("Genre: %d (%s)\n", tag.genre, get_genre_name(tag.genre));
}