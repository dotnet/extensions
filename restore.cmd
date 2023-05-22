@echo off
SETLOCAL

set _args=%*
if "%~1"=="-?" set _args=-help
if "%~1"=="/?" set _args=-help

powershell -ExecutionPolicy ByPass -NoProfile -command "& """%~dp0eng\build.ps1""" -restore %_args%"
exit /b %ErrorLevel%