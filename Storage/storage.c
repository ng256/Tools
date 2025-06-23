/******************************************************************************
File: storage.cgi  
Description: Secure key-value CGI storage

Copyright (C) Pavel Bashkardin  
Permission is hereby granted, free of charge, to any person obtaining a copy of this software  
and associated documentation files (the "Software"), to deal in the Software without restriction,  
including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense,  
and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so,  
subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies  
or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,  
INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,  
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.  
IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES  
OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE,  
ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER  
DEALINGS IN THE SOFTWARE.
******************************************************************************/

#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <unistd.h>
#include <sys/stat.h>
#include <fcntl.h>
#include <ctype.h>
#include <sys/file.h>

#define DATA_DIR "/tmp/storagecgi"
#define LOCK_FILE "/tmp/storagecgi/.lock"

#define MAX_KEY 256
#define MAX_VAL 8192
#define MAX_PATH 512

// Check whether the given key is valid
int is_valid_key(const char *key) {
    if (!key || *key == '\0') return 0;
    while (*key) {
        if (!(isalnum((unsigned char)*key) || *key == '-' || *key == '_')) {
            return 0;
        }
        key++;
    }
    return 1;
}

// Decode URL-encoded string into dest, limiting max_len
void url_decode(const char *src, char *dest, size_t max_len) {
    size_t count = 0;
    while (*src && count < max_len - 1) {
        if (*src == '+') {
            *dest++ = ' ';
        } else if (*src == '%' && isxdigit((unsigned char)src[1]) && isxdigit((unsigned char)src[2])) {
            char hex[3] = {src[1], src[2], '\0'};
            *dest++ = (char)strtol(hex, NULL, 16);
            src += 2;
        } else {
            *dest++ = *src;
        }
        src++;
        count++;
    }
    *dest = '\0';
}

// Ensure data directory exists and is writable
void ensure_data_dir() {
    struct stat st = {0};
    if (stat(DATA_DIR, &st) == -1) {
        if (mkdir(DATA_DIR, 0700) != 0) {
            perror("Failed to create data directory");
            exit(EXIT_FAILURE);
        }
    }
    if (access(DATA_DIR, W_OK) != 0) {
        perror("Data directory not writable");
        exit(EXIT_FAILURE);
    }
}

// Sanitize key and ensure it's a safe filename
void get_safe_path(char *path, size_t max_path, const char *key) {
    if (strstr(key, "..") || strchr(key, '/')) {
        fprintf(stderr, "Invalid key format\n");
        exit(EXIT_FAILURE);
    }
    if (snprintf(path, max_path, DATA_DIR "%s", key) >= (int)max_path) {
        fprintf(stderr, "Path too long\n");
        exit(EXIT_FAILURE);
    }
}

// Send standard secure headers
void send_header() {
    printf("Content-Type: text/plain; charset=UTF-8\r\n");
    printf("Cache-Control: no-store\r\n");
    printf("X-Content-Type-Options: nosniff\r\n");
    printf("X-Frame-Options: DENY\r\n");
    printf("\r\n");
}

// Function to set a lock
int acquire_lock(int lock_type) {
    int lock_fd = open(LOCK_FILE, O_RDWR | O_CREAT, 0600);
    if (lock_fd == -1) {
        return -1;
    }

    if (flock(lock_fd, lock_type) != 0) {
        close(lock_fd);
        return -1;
    }
    return lock_fd;
}

// Function to release a lock
void release_lock(int lock_fd) {
    if (lock_fd != -1) {
        flock(lock_fd, LOCK_UN);
        close(lock_fd);
    }
}

// Handle GET requests
void handle_get() {
    int lock_fd = acquire_lock(LOCK_SH);
    if (lock_fd == -1) {
        printf("Lock error\n");
        return;
    }

    char *query = getenv("QUERY_STRING");
    if (!query || strlen(query) == 0) {
        printf("No key provided\n");
        release_lock(lock_fd);
        return;
    }

    char key_enc[MAX_KEY], key[MAX_KEY];
    char *key_start = strstr(query, "key=");
    if (!key_start) {
        printf("Missing key parameter\n");
        release_lock(lock_fd);
        return;
    }
    key_start += 4;

    char *key_end = strchr(key_start, '&');
    size_t key_len = key_end ? (size_t)(key_end - key_start) : strlen(key_start);
    if (key_len >= MAX_KEY) {
        printf("Key too long\n");
        release_lock(lock_fd);
        return;
    }

    strncpy(key_enc, key_start, key_len);
    key_enc[key_len] = '\0';

    url_decode(key_enc, key, sizeof(key));
    if (!is_valid_key(key)) {
        printf("Invalid key format\n");
        release_lock(lock_fd);
        return;
    }

    char path[MAX_PATH];
    get_safe_path(path, sizeof(path), key);

    FILE *f = fopen(path, "r");
    if (!f) {
        printf("Not found\n");
        release_lock(lock_fd);
        return;
    }

    char value[MAX_VAL];
    size_t nread = fread(value, 1, MAX_VAL - 1, f);
    value[nread] = '\0';
    fclose(f);

    for (char *p = value; *p; p++) {
        if (!isprint((unsigned char)*p) && !isspace((unsigned char)*p)) {
            *p = '?';
        }
    }

    printf("%s\n", value);
    release_lock(lock_fd);
}

// Handle POST requests
void handle_post() {
    int lock_fd = acquire_lock(LOCK_EX);
    if (lock_fd == -1) {
        printf("Lock error\n");
        return;
    }

    char *len_str = getenv("CONTENT_LENGTH");
    if (!len_str) {
        printf("Missing CONTENT_LENGTH\n");
        release_lock(lock_fd);
        return;
    }

    long len = strtol(len_str, NULL, 10);
    if (len <= 0 || len > MAX_KEY + MAX_VAL + 64) {
        printf("Invalid content length\n");
        release_lock(lock_fd);
        return;
    }

    char *data = malloc(len + 1);
    if (!data) {
        printf("Memory allocation failed\n");
        release_lock(lock_fd);
        return;
    }

    if (fread(data, 1, len, stdin) != (size_t)len) {
        printf("Read error\n");
        free(data);
        release_lock(lock_fd);
        return;
    }
    data[len] = '\0';

    char *key_start = strstr(data, "key=");
    char *val_start = strstr(data, "value=");
    if (!key_start || !val_start) {
        printf("Missing parameters\n");
        free(data);
        release_lock(lock_fd);
        return;
    }
    key_start += 4;
    val_start += 6;

    char *key_end = strchr(key_start, '&');
    size_t key_len = key_end ? (size_t)(key_end - key_start) : strlen(key_start);
    if (key_len >= MAX_KEY) {
        printf("Key too long\n");
        free(data);
        release_lock(lock_fd);
        return;
    }

    char key_enc[MAX_KEY], val_enc[MAX_VAL];
    strncpy(key_enc, key_start, key_len);
    key_enc[key_len] = '\0';

    strncpy(val_enc, val_start, MAX_VAL - 1);
    val_enc[MAX_VAL - 1] = '\0';

    char key[MAX_KEY], value[MAX_VAL];
    url_decode(key_enc, key, sizeof(key));
    url_decode(val_enc, value, sizeof(value));

    if (!is_valid_key(key)) {
        printf("Invalid key format\n");
        free(data);
        release_lock(lock_fd);
        return;
    }

    char path[MAX_PATH];
    get_safe_path(path, sizeof(path), key);

    char tmp_path[MAX_PATH + 16];
    snprintf(tmp_path, sizeof(tmp_path), "%s.tmp%d", path, rand());

    int fd = open(tmp_path, O_WRONLY | O_CREAT | O_EXCL, 0600);
    if (fd == -1) {
        printf("File creation failed\n");
        free(data);
        release_lock(lock_fd);
        return;
    }

    FILE *f = fdopen(fd, "w");
    if (!f) {
        close(fd);
        unlink(tmp_path);
        printf("File open failed\n");
        free(data);
        release_lock(lock_fd);
        return;
    }

    if (fprintf(f, "%s", value) < 0) {
        fclose(f);
        unlink(tmp_path);
        printf("Write error\n");
        free(data);
        release_lock(lock_fd);
        return;
    }
    fclose(f);

    if (rename(tmp_path, path) != 0) {
        unlink(tmp_path);
        printf("File rename failed\n");
        free(data);
        release_lock(lock_fd);
        return;
    }

    printf("OK\n");
    free(data);
    release_lock(lock_fd);
}

int main() {
    ensure_data_dir();
    send_header();
    srand(getpid());

    char *method = getenv("REQUEST_METHOD");
    if (!method) {
        printf("Missing REQUEST_METHOD\n");
        return EXIT_FAILURE;
    }

    if (strcmp(method, "GET") == 0) {
        handle_get();
    } else if (strcmp(method, "POST") == 0) {
        handle_post();
    } else {
        printf("Unsupported method: %s\n", method);
        return EXIT_FAILURE;
    }

    return EXIT_SUCCESS;
}
