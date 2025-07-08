#include "id3v2.h"
#include "utils.h"
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <ctype.h>

int syncsafe_to_size(const unsigned char size[4]) {
    return (size[0] << 21) | (size[1] << 14) | (size[2] << 7) | size[3];
}

static void print_text(unsigned char encoding, const unsigned char *text, int text_len) {
    if (text_len <= 0) return;

    if (encoding == ENCODING_ISO8859) { // ISO-8859-1
        printf("%.*s", text_len, text);
    }
    else if (encoding == ENCODING_UTF16) { // UTF-16 with BOM
        if (text_len < 2) return;
        
        // Check Byte Order Mark
        int is_big_endian = 0;
        if (text[0] == 0xFF && text[1] == 0xFE) {
            is_big_endian = 0; // Little endian
            text += 2;
            text_len -= 2;
        } 
        else if (text[0] == 0xFE && text[1] == 0xFF) {
            is_big_endian = 1; // Big endian
            text += 2;
            text_len -= 2;
        }
        
        // Print ASCII characters
        for (int i = 0; i < text_len; i += 2) {
            if (i + 1 >= text_len) break;
            
            if (is_big_endian) {
                if (text[i] == 0) {
                    printf("%c", text[i + 1]);
                }
            } else {
                if (text[i + 1] == 0) {
                    printf("%c", text[i]);
                }
            }
        }
    }
    else if (encoding == ENCODING_UTF8) { // UTF-8
        printf("%.*s", text_len, text);
    }
    else {
        printf("(unknown encoding)");
    }
}

static void process_comment_frame(unsigned char encoding, unsigned char **text, int *text_len) {
    // Skip language (3 bytes)
    if (*text_len > 3) {
        *text += 3;
        *text_len -= 3;
    }
    
    // Find end of description
    int desc_len = 0;
    if (encoding == ENCODING_ISO8859 || encoding == ENCODING_UTF8) {
        while (desc_len < *text_len && (*text)[desc_len] != 0) {
            desc_len++;
        }
        if (desc_len < *text_len) desc_len++; // skip null terminator
    } 
    else if (encoding == ENCODING_UTF16) {
        while (desc_len + 1 < *text_len && 
              !((*text)[desc_len] == 0 && (*text)[desc_len + 1] == 0)) {
            desc_len += 2;
        }
        if (desc_len + 1 < *text_len) desc_len += 2; // skip double null
    }
    
    *text += desc_len;
    *text_len -= desc_len;
}

static void process_genre_frame(unsigned char encoding, const unsigned char *text, int text_len) {
    // Check for numeric genre format (XX) or (XXX)
    if (text_len >= 3 && text[0] == '(' && isdigit(text[1])) {
        // Extract numeric part
        char num_str[4] = {0};
        int num_len = 0;
        int i = 1;
        while (i < text_len && num_len < 3 && isdigit(text[i])) {
            num_str[num_len++] = text[i++];
        }
        
        if (num_len > 0) {
            int genre_id = atoi(num_str);
            printf("%s", get_genre_name(genre_id));
        } else {
            print_text(encoding, text, text_len);
        }
    } else {
        print_text(encoding, text, text_len);
    }
}

void print_id3v2_tags(FILE *f) {
    ID3v2Header header;
    fseek(f, 0, SEEK_SET);
    if (fread(&header, sizeof(header), 1, f) != 1) {
        printf("Error reading ID3v2 header\n");
        return;
    }
    if (strncmp(header.id, "ID3", 3) != 0) {
        printf("ID3v2 tag not found\n");
        return;
    }
    
    int tag_size = syncsafe_to_size(header.size);
    printf("ID3v2 tag found: version 2.%d.%d, size %d bytes\n", 
           header.ver, header.rev, tag_size);
    
    unsigned char *tag_data = malloc(tag_size);
    if (!tag_data) {
        printf("Memory allocation error\n");
        return;
    }
    
    if (fread(tag_data, 1, tag_size, f) != (size_t)tag_size) {
        printf("Error reading tag data\n");
        free(tag_data);
        return;
    }
    
    int pos = 0;
    while (pos + 10 <= tag_size) {
        char frame_id[MAX_FRAME_ID_SIZE] = {0};
        memcpy(frame_id, tag_data + pos, 4);
        if (frame_id[0] == 0) break;
        
        unsigned int frame_size = (tag_data[pos + 4] << 24) | 
                                 (tag_data[pos + 5] << 16) |
                                 (tag_data[pos + 6] << 8) | 
                                 tag_data[pos + 7];
        if (frame_size == 0) break;
        if (pos + 10 + frame_size > tag_size) break;
        
        unsigned char encoding = tag_data[pos + 10];
        unsigned char *text = tag_data + pos + 11;
        int text_len = frame_size - 1;
        
        printf("%s: ", frame_id);
        
        if (strcmp(frame_id, FRAME_COMMENT) == 0) {
            process_comment_frame(encoding, &text, &text_len);
            print_text(encoding, text, text_len);
        }
        else if (strcmp(frame_id, FRAME_GENRE) == 0) {
            process_genre_frame(encoding, text, text_len);
        }
        else {
            print_text(encoding, text, text_len);
        }
        
        printf("\n");
        pos += 10 + frame_size;
    }
    free(tag_data);
}