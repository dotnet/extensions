#!/bin/bash

echo "user service status"
systemctl status user@vsts.service

echo "start user service"
systemctl start user@vsts.service

echo "is-system-running"
systemctl is-system-running

echo "env"
env

echo "ps"
ps aux

echo "is-system-running --user"
systemctl --user is-system-running

echo "systemctl --user"
systemctl --user --no-pager

exit 1