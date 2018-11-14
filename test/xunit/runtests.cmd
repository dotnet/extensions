set target=%1
%HELIX_CORRELATION_PAYLOAD%\net461\xunit.console.exe %target% -xml testResults.xml
