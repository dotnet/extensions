@ECHO OFF
SETLOCAL

if not [%1] == [] (set remote_repo=%1) else (set remote_repo=%AZUREEXTENSIONS_REPO%)

IF [%remote_repo%] == [] (
  echo The 'AZUREEXTENSIONS_REPO' environment variable or command line parameter is not set, aborting.
  exit /b 1
)

echo AZUREEXTENSIONS_REPO: %remote_repo%

REM https://superuser.com/questions/280425/getting-robocopy-to-return-a-proper-exit-code
IF %ERRORLEVEL% LSS 8 (robocopy ..\Data.Validation\ %remote_repo%\src\Shared\Data.Validation\ /MIR)
IF %ERRORLEVEL% LSS 8 (robocopy ..\AzureSync\ %remote_repo%\src\Shared\DotNetSync\ /MIR)
IF %ERRORLEVEL% LSS 8 (robocopy ..\EmptyCollections\ %remote_repo%\src\Shared\EmptyCollections\ /MIR)
IF %ERRORLEVEL% LSS 8 (robocopy ..\NumericExtensions\ %remote_repo%\src\Shared\NumericExtensions\ /MIR)
IF %ERRORLEVEL% LSS 8 (robocopy ..\Throw\ %remote_repo%\src\Shared\Throw\ /MIR)

rem 0x0-4 mean everythings good, files may have been copied. 0x08-10 mean are actual errors.
echo ErrorLevel = %ERRORLEVEL%
IF %ERRORLEVEL% LSS 8 (exit /b 0) ELSE (exit 1)
