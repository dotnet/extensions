#!/bin/sh

set -e

# This is a simple script primarily used for CI to install necessary dependencies
#
# Usage:
#
# ./install-native-dependencies.sh


# Ubuntu 22.04 doesn't have OpenSSL 1.x, which is required for .NET Core 3.1

# download binary openssl packages from Impish builds
wget http://security.ubuntu.com/ubuntu/pool/main/o/openssl/openssl_1.1.1f-1ubuntu2.19_amd64.deb
wget http://security.ubuntu.com/ubuntu/pool/main/o/openssl/libssl-dev_1.1.1f-1ubuntu2.19_amd64.deb
wget http://security.ubuntu.com/ubuntu/pool/main/o/openssl/libssl1.1_1.1.1f-1ubuntu2.19_amd64.deb

# install downloaded binary packages
sudo dpkg -i libssl1.1_1.1.1f-1ubuntu2.19_amd64.deb
sudo dpkg -i libssl-dev_1.1.1f-1ubuntu2.19_amd64.deb
sudo dpkg -i openssl_1.1.1f-1ubuntu2.19_amd64.deb
