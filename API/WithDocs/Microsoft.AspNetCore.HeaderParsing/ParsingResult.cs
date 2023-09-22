// Assembly 'Microsoft.AspNetCore.HeaderParsing'

namespace Microsoft.AspNetCore.HeaderParsing;

/// <summary>
/// Result of trying to parse a header.
/// </summary>
public enum ParsingResult
{
    /// <summary>
    /// Indicates the header was successfully parsed.
    /// </summary>
    Success = 0,
    /// <summary>
    /// Indicates the header's value was malformed and couldn't be parsed.
    /// </summary>
    Error = 1,
    /// <summary>
    /// Indicates the header was not present.
    /// </summary>
    NotFound = 2
}
