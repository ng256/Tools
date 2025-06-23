/************************************************************
 * File: storage-sync.c
 * Description: Synchronizes volatile CGI storage in /tmp with
 *              persistent storage in /etc, using file-level locking.
 *
 * Copyright (C) Pavel Bashkardin
 * MIT License applies - see LICENSE file for details.
 ************************************************************/

#include <stdio.h>
#include <stdlib.h>
#include <unistd.h>
#include <sys/stat.h>
#include <sys/file.h>
#include <dirent.h>
#include <string.h>
#include <errno.h>

#define TMP_DIR "/tmp/storagecgi"
#define PERSISTENT_DIR "/etc/storagecgi"
#define LOCK_FILE "/tmp/storagecgi/.lock"
#define MAX_PATH 512

// Recursively remove directory contents
int remove_directory(const char *path) {
    DIR *dir = opendir(path);
    if (!dir) return -1;

    struct dirent *entry;
    char fullpath[MAX_PATH];
    while ((entry = readdir(dir))) {
        if (!strcmp(entry->d_name, ".") || !strcmp(entry->d_name, ".."))
            continue;
        snprintf(fullpath, sizeof(fullpath), "%s/%s", path, entry->d_name);
        if (entry->d_type == DT_DIR) {
            remove_directory(fullpath);
            rmdir(fullpath);
        } else {
            unlink(fullpath);
        }
    }
    closedir(dir);
    return 0;
}

// Copy single file from src to dst
int copy_file(const char *src, const char *dst) {
    FILE *f_src = fopen(src, "rb");
    if (!f_src) return -1;
    FILE *f_dst = fopen(dst, "wb");
    if (!f_dst) {
        fclose(f_src);
        return -1;
    }

    char buf[4096];
    size_t n;
    while ((n = fread(buf, 1, sizeof(buf), f_src)) > 0) {
        fwrite(buf, 1, n, f_dst);
    }

    fclose(f_src);
    fclose(f_dst);
    return 0;
}

// Recursively copy directory contents
int copy_directory(const char *src, const char *dst) {
    DIR *dir = opendir(src);
    if (!dir) return -1;

    mkdir(dst, 0755);
    struct dirent *entry;
    char srcpath[MAX_PATH], dstpath[MAX_PATH];

    while ((entry = readdir(dir))) {
        if (!strcmp(entry->d_name, ".") || !strcmp(entry->d_name, ".."))
            continue;

        snprintf(srcpath, sizeof(srcpath), "%s/%s", src, entry->d_name);
        snprintf(dstpath, sizeof(dstpath), "%s/%s", dst, entry->d_name);

        if (entry->d_type == DT_DIR) {
            copy_directory(srcpath, dstpath);
        } else {
            copy_file(srcpath, dstpath);
        }
    }
    closedir(dir);
    return 0;
}

// Check if directory exists and is non-empty
int is_non_empty_dir(const char *path) {
    DIR *dir = opendir(path);
    if (!dir) return 0;
    struct dirent *entry;
    while ((entry = readdir(dir))) {
        if (strcmp(entry->d_name, ".") && strcmp(entry->d_name, "..")) {
            closedir(dir);
            return 1;
        }
    }
    closedir(dir);
    return 0;
}

int main() {
    mkdir(TMP_DIR, 0755);
    mkdir(PERSISTENT_DIR, 0755);

    int lock_fd = open(LOCK_FILE, O_CREAT | O_RDWR, 0666);
    if (lock_fd < 0) {
        perror("open lock");
        return 1;
    }

    if (flock(lock_fd, LOCK_EX) < 0) {
        perror("flock");
        close(lock_fd);
        return 1;
    }

    // If TMP_DIR is empty and PERSISTENT_DIR exists, restore
    if (!is_non_empty_dir(TMP_DIR) && is_non_empty_dir(PERSISTENT_DIR)) {
        copy_directory(PERSISTENT_DIR, TMP_DIR);
    }

    // Remove old persistent files
    remove_directory(PERSISTENT_DIR);
    mkdir(PERSISTENT_DIR, 0755);

    // Copy current tmp data to persistent storage
    copy_directory(TMP_DIR, PERSISTENT_DIR);

    flock(lock_fd, LOCK_UN);
    close(lock_fd);

    return 0;
}
