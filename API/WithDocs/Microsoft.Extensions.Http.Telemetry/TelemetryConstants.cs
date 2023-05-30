// Assembly 'Microsoft.Extensions.Telemetry.Abstractions'

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions.Http.Telemetry;

/// <summary>
/// Common telemetry constants used by various telemetry libraries.
/// </summary>
public static class TelemetryConstants
{
    /// <summary>
    /// Request metadata key that is used when storing request metadata object.
    /// </summary>
    public const string RequestMetadataKey = "R9-RequestMetadata";

    /// <summary>
    /// Placeholder string for unknown request name, dependency name etc. in telemetry.
    /// </summary>
    public const string Unknown = "unknown";

    /// <summary>
    /// Placeholder string used for redacted data where needed.
    /// </summary>
    public const string Redacted = "REDACTED";

    /// <summary>
    /// Header for client application name, sent on an outgoing http call.
    /// </summary>
    [Experimental("EXTEXP0003", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
    public const string ClientApplicationNameHeader = "X-ClientApplication";

    /// <summary>
    /// Header for server application name, sent on a http request.
    /// </summary>
    [Experimental("EXTEXP0003", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
    public const string ServerApplicationNameHeader = "X-ServerApplication";
}
