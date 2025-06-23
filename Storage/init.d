#!/bin/sh /etc/rc.common

# #############################################################
# File: storage-sync.init
# Description: Init script to sync storage between /tmp and /etc
#
# Copyright (C) Pavel Bashkardin
# MIT License applies - see LICENSE file for details.
# #############################################################

START=99
STOP=10

start() {
    echo "Starting storage-sync..."
    /usr/bin/storage-sync
}

stop() {
    echo "Stopping storage-sync (noop)..."
    # This sync is one-shot, nothing to stop
}

restart() {
    stop
    start
}
