// Assembly 'Microsoft.Extensions.Http.Telemetry'

namespace Microsoft.Extensions.Http.Telemetry.Logging;

/// <summary>
/// Strategy to decide how outgoing HTTP path is logged.
/// </summary>
public enum OutgoingPathLoggingMode
{
    /// <summary>
    /// HTTP path is formatted, for example in a form of /foo/bar/redactedUserId.
    /// </summary>
    Formatted = 0,
    /// <summary>
    /// HTTP path is not formatted, route parameters logged in curly braces, for example in a form of /foo/bar/{userId}.
    /// </summary>
    Structured = 1
}
