// Assembly 'Microsoft.Extensions.Http.Telemetry'

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions.Http.Telemetry.Metering;

[Experimental("EXTEXP0003", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
public enum HttpRequestResultType
{
    Success = 0,
    Failure = 1,
    ExpectedFailure = 2
}
