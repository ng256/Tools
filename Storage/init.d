#!/bin/sh /etc/rc.common

START=99
STOP=10
USE_PROCD=1

start_service() {
    procd_open_instance
    procd_set_param command /usr/bin/storage-sync
    procd_set_param respawn
    procd_close_instance
}
