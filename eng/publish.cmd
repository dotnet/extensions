rem workaround for dotnet/arcade#1425
@echo off
powershell -ExecutionPolicy ByPass -NoProfile -command "& """%~dp0common\Build.ps1""" -publish -ci %*"
exit /b %ErrorLevel%
