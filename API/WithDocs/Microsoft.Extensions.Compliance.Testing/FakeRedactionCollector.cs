// Assembly 'Microsoft.Extensions.Compliance.Testing'

using System.Collections.Generic;

namespace Microsoft.Extensions.Compliance.Testing;

/// <summary>
/// Usage history of fake redaction types.
/// </summary>
public class FakeRedactionCollector
{
    /// <summary>
    /// Gets the last redactor request "event".
    /// </summary>
    /// <exception cref="T:System.InvalidOperationException">When there has been no previous redactor request event.</exception>
    public RedactorRequested LastRedactorRequested { get; }

    /// <summary>
    /// Gets the full log of all redactor request events that happened.
    /// </summary>
    public IReadOnlyList<RedactorRequested> AllRedactorRequests { get; }

    /// <summary>
    /// Gets the last redaction "event".
    /// </summary>
    /// <exception cref="T:System.InvalidOperationException">When there has been no previous redaction event.</exception>
    public RedactedData LastRedactedData { get; }

    /// <summary>
    /// Gets the full log of all redaction events that happened.
    /// </summary>
    public IReadOnlyList<RedactedData> AllRedactedData { get; }

    public FakeRedactionCollector();
}
