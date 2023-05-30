// Assembly 'Microsoft.Extensions.Compliance.Redaction'

using System.Runtime.CompilerServices;
using Microsoft.Extensions.Compliance.Classification;

namespace Microsoft.Extensions.Compliance.Redaction;

/// <summary>
/// A provider that only returns the null redactor implementation used for situations that don't require redaction.
/// </summary>
public sealed class NullRedactorProvider : IRedactorProvider
{
    /// <summary>
    /// Gets the singleton instance of this class.
    /// </summary>
    public static NullRedactorProvider Instance { get; }

    /// <inheritdoc />
    public Redactor GetRedactor(DataClassification classification);

    public NullRedactorProvider();
}
