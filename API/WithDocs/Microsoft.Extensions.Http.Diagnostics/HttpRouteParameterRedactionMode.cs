// Assembly 'Microsoft.Extensions.Diagnostics.ExtraAbstractions'

namespace Microsoft.Extensions.Http.Diagnostics;

/// <summary>
/// Strategy to decide how HTTP request path parameters are redacted.
/// </summary>
public enum HttpRouteParameterRedactionMode
{
    /// <summary>
    /// All parameters are considered as sensitive and are required to be explicitly annotated with a data classification.
    /// </summary>
    /// <remarks>
    /// Unannotated parameters are always redacted with the erasing redactor.
    /// </remarks>
    Strict = 0,
    /// <summary>
    /// All parameters are considered as non-sensitive and included as-is by default.
    /// </summary>
    /// <remarks>
    /// Only parameters explicitly annotated with a data classification are redacted.
    /// </remarks>
    Loose = 1,
    /// <summary>
    /// Route parameters are not redacted regardless of the presence of data classification annotations.
    /// </summary>
    None = 2
}
