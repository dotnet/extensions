@echo off
powershell -ExecutionPolicy ByPass -NoProfile -command "& """%~dp0..\common\Build.ps1""" -test /p:RunFlakyTests=true %*"
exit /b %ErrorLevel%