#include "id3v2.h"
#include "id3v1.h"
#include <stdio.h>
#include <stdlib.h>
#include <string.h>

int main(int argc, char **argv) {
    if (argc != 2) {
        printf("Usage: %s <mp3 file>\n", argv[0]);
        return 1;
    }
    
    FILE *f = fopen(argv[1], "rb");
    if (!f) {
        perror("Failed to open file");
        return 1;
    }
    
    // Check for ID3v2 tag
    char id3_header[3] = {0};
    fread(id3_header, 1, 3, f);
    fseek(f, 0, SEEK_SET);
    
    if (strncmp(id3_header, "ID3", 3) == 0) {
        print_id3v2_tags(f);
    } else {
        print_id3v1_tags(f);
    }
    
    fclose(f);
    return 0;
}