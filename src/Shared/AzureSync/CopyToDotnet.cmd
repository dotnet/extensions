@ECHO OFF
SETLOCAL

if not [%1] == [] (set remote_repo=%1) else (set remote_repo=%DOTNETEXTENSIONS_REPO%)

IF [%remote_repo%] == [] (
  echo The 'DOTNETEXTENSIONS_REPO' environment variable or command line parameter is not set, aborting.
  exit /b 1
)

echo DOTNETEXTENSIONS_REPO: %remote_repo%

REM https://superuser.com/questions/280425/getting-robocopy-to-return-a-proper-exit-code
(robocopy ..\Data.Validation\ %remote_repo%\src\Shared\Data.Validation\ /MIR) ^& IF %ERRORLEVEL% LSS 8 SET ERRORLEVEL = 0
(robocopy ..\DotNetSync\ %remote_repo%\src\Shared\AzureSync\ /MIR) ^& IF %ERRORLEVEL% LSS 8 SET ERRORLEVEL = 0
(robocopy ..\EmptyCollections\ %remote_repo%\src\Shared\EmptyCollections\ /MIR) ^& IF %ERRORLEVEL% LSS 8 SET ERRORLEVEL = 0
(robocopy ..\NumericExtensions\ %remote_repo%\src\Shared\NumericExtensions\ /MIR) ^& IF %ERRORLEVEL% LSS 8 SET ERRORLEVEL = 0
(robocopy ..\Throw\ %remote_repo%\src\Shared\Throw\ /MIR) ^& IF %ERRORLEVEL% LSS 8 SET ERRORLEVEL = 0
