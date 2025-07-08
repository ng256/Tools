#ifndef UTILS_H
#define UTILS_H

// ID3v1 genre list (0-147)
extern const char* const genres[];
extern const int GENRES_COUNT;

const char* get_genre_name(int genre_id);

// Constants
#define ID3V2_HEADER_SIZE 10
#define ID3V1_TAG_SIZE 128
#define MAX_FRAME_ID_SIZE 5
#define MAX_GENRE_NAME_SIZE 50

#endif // UTILS_H