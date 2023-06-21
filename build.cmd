@ECHO OFF

SET _args=%*
IF "%~1"=="-?" SET _args=-help
IF "%~1"=="/?" SET _args=-help

IF ["%_args%"] == [""] (
    :: Perform restore and build, IF no args are supplied.
    SET _args=-restore -build
)

powershell -ExecutionPolicy ByPass -NoProfile -command "& """%~dp0eng\build.ps1""" %_args%"
EXIT /b %ERRORLEVEL%
