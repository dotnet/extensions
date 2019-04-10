@echo off
powershell -ExecutionPolicy ByPass -NoProfile -command "& """%~dp0..\common\Build.ps1""" -test -ci /p:RunFlakyTests=true %*"
exit /b %ErrorLevel%