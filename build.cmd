@ECHO OFF
SETLOCAL EnableDelayedExpansion

SET _args=%*
IF "%~1"=="-?" SET _args=-help
IF "%~1"=="/?" SET _args=-help

IF ["%_args%"] == [""] (
    :: Perform restore and build, IF no args are supplied.
    SET _args=-restore -build
)

FOR %%x IN (%*) DO (
    SET _arg=%%x

    IF /I "%%x%"=="-coverage" GOTO RUN_CODE_COVERAGE
)

powershell -ExecutionPolicy ByPass -NoProfile -command "& """%~dp0eng\build.ps1""" %_args%"
EXIT /b %ERRORLEVEL%


:RUN_CODE_COVERAGE
    SET DOTNET_ROOT=%~dp0.dotnet
    :: This tells .NET Core not to go looking for .NET Core in other places
    SET DOTNET_MULTILEVEL_LOOKUP=0

    dotnet dotnet-coverage collect --settings ./eng/CodeCoverage.config --output ./artifacts/TestResults/ "build.cmd -test -bl"
    dotnet reportgenerator -reports:./artifacts/TestResults/*.cobertura.xml -targetdir:./artifacts/TestResults/CoverageResultsHtml -reporttypes:HtmlInline_AzurePipelines
    start ./artifacts/TestResults/CoverageResultsHtml/index.html
    powershell -ExecutionPolicy ByPass -NoProfile -command "./scripts/ValidateProjectCoverage.ps1 -CoberturaReportXml ./artifacts/TestResults/.cobertura.xml"
    EXIT /b %ErrorLevel%
