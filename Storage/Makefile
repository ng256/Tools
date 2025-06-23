# ############################################################
# File: Makefile
# Description: Builds storage-sync utility
#
# Copyright (C) Pavel Bashkardin
# MIT License applies - see LICENSE file for details.
# ############################################################

CC = gcc
CFLAGS = -Os -Wall
TARGET = storage-sync

all: $(TARGET)

$(TARGET): storage-sync.c
	$(CC) $(CFLAGS) -o $(TARGET) storage-sync.c

install:
	install -m 0755 $(TARGET) /usr/bin/$(TARGET)
	install -m 0755 storage-sync.init /etc/init.d/storage-sync
	/etc/init.d/storage-sync enable

clean:
	rm -f $(TARGET)
