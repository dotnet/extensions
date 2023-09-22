// Assembly 'Microsoft.Extensions.Compliance.Redaction'

using System.Runtime.CompilerServices;

namespace Microsoft.Extensions.Compliance.Redaction;

/// <summary>
/// Options for the XxHash3 redactor.
/// </summary>
public class XxHash3RedactorOptions
{
    /// <summary>
    /// Gets or sets a hash seed used when computing hashes during redaction.
    /// </summary>
    /// <value>
    /// The default value is 0.
    /// </value>
    /// <remarks>
    /// You typically pick a unique value for your application and don't change it afterwards. You'll want a different value for
    /// different deployment environments in order to prevent identifiers from one environment being redacted to the same
    /// value across environments.
    /// </remarks>
    public ulong HashSeed { get; set; }

    public XxHash3RedactorOptions();
}
