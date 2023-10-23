// Assembly 'Microsoft.AspNetCore.Diagnostics.Middleware'

namespace Microsoft.AspNetCore.Diagnostics.Logging;

/// <summary>
/// Strategy to decide how request path is logged.
/// </summary>
public enum IncomingPathLoggingMode
{
    /// <summary>
    /// Request path is logged formatted, its params are not logged.
    /// </summary>
    Formatted = 0,
    /// <summary>
    /// Request path is logged in a structured way (as route), its params are logged.
    /// </summary>
    Structured = 1
}
