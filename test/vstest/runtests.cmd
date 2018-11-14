set target=%1
set version=%2
powershell.exe -NoProfile -ExecutionPolicy unrestricted -Command "[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12; &([scriptblock]::Create((Invoke-WebRequest -useb 'https://dot.net/v1/dotnet-install.ps1'))) -Version %version% -Channel 2.0 -NoCdn -InstallDir %HELIX_CORRELATION_PAYLOAD%\sdk"
set DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
%HELIX_CORRELATION_PAYLOAD%\sdk\dotnet vstest %target% --logger:trx


