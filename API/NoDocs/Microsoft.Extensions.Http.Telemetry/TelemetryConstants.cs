// Assembly 'Microsoft.Extensions.Telemetry.Abstractions'

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions.Http.Telemetry;

public static class TelemetryConstants
{
    public const string RequestMetadataKey = "R9-RequestMetadata";
    public const string Unknown = "unknown";
    public const string Redacted = "REDACTED";
    [Experimental("EXTEXP0003", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
    public const string ClientApplicationNameHeader = "X-ClientApplication";
    [Experimental("EXTEXP0003", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
    public const string ServerApplicationNameHeader = "X-ServerApplication";
}
