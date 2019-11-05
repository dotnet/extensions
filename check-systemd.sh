#!/bin/bash

echo "is-system-running"
systemctl is-system-running

echo "is-system-running --user"
systemctl --user is-system-running

echo "systemctl --user"
systemctl --user --no-pager

exit 1