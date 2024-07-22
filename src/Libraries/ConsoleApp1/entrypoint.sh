#!/bin/sh

#apk add dotnet8-sdk

echo "Environment Variables:"
printenv

echo "Starting service binary..."
dotnet ConsoleApp1.dll
