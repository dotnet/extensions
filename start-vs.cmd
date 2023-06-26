@echo off
setlocal enabledelayedexpansion

:: This command launches a Visual Studio solution with environment variables required to use a local version of the .NET Core SDK.

:: This tells .NET Core to use the same dotnet.exe that build scripts use
set DOTNET_ROOT=%~dp0.dotnet
set DOTNET_ROOT(x86)=%~dp0.dotnet\x86

:: This tells .NET Core not to go looking for .NET Core in other places
set DOTNET_MULTILEVEL_LOOKUP=0

:: Put our local dotnet.exe on PATH first so Visual Studio knows which one to use
set PATH=%DOTNET_ROOT%;%PATH%

set SLN=SDK.sln
if not exist "%~dp0%SLN%" (
    echo [41m[ERROR][0m %~dp0%SLN% not found.
    echo See [93m%~dp0%docs\building.md[0m for instructions on how to generate a solution file.
    echo.
    exit /b 1
)

call restore.cmd

if not exist "%DOTNET_ROOT%\dotnet.exe" (
    echo [41m[ERROR][0m .NET SDK has not yet been installed. Run [93m%~dp0restore.cmd[0m to install.
    echo.
    exit /b 1
)

:: Prefer the VS in the developer command prompt if we're in one, followed by whatever shows up in the current search path.
set "DEVENV=%DevEnvDir%devenv.exe"

if exist "%DEVENV%" (
    :: Fully qualified works
    set "COMMAND=start "" /B "%ComSpec%" /S /C ""%DEVENV%" "%~dp0%SLN%"""
) else (
    where devenv.exe /Q
    if !errorlevel! equ 0 (
        :: On the PATH, use that.
        set "COMMAND=start "" /B "%ComSpec%" /S /C "devenv.exe "%~dp0%SLN%"""
    ) else (
        :: Can't find devenv.exe, let file associations take care of it
        set "COMMAND=start /B .\%SLN%"
    )
)

%COMMAND%
