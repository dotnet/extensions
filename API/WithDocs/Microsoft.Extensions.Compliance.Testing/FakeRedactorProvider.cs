// Assembly 'Microsoft.Extensions.Compliance.Testing'

using System.Runtime.CompilerServices;
using Microsoft.Extensions.Compliance.Classification;
using Microsoft.Extensions.Compliance.Redaction;

namespace Microsoft.Extensions.Compliance.Testing;

/// <summary>
/// A provider of fake redactors.
/// </summary>
public class FakeRedactorProvider : IRedactorProvider
{
    /// <summary>
    /// Gets the collector that stores data about usage of fake redaction classes.
    /// </summary>
    public FakeRedactionCollector Collector { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="T:Microsoft.Extensions.Compliance.Testing.FakeRedactorProvider" /> class.
    /// </summary>
    /// <param name="options">Fake redactor options.</param>
    /// <param name="eventCollector">Collects information about redactor requests.</param>
    public FakeRedactorProvider(FakeRedactorOptions? options = null, FakeRedactionCollector? eventCollector = null);

    /// <inheritdoc />
    public Redactor GetRedactor(DataClassification classification);
}
