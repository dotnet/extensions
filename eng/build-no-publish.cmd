rem workaround for dotnet/arcade#1425
@echo off
powershell -ExecutionPolicy ByPass -NoProfile -command "& """%~dp0common\Build.ps1""" -restore -build -test -sign -pack -ci %*"
exit /b %ErrorLevel%
