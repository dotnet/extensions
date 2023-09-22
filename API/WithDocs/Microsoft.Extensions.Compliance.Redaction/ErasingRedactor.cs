// Assembly 'Microsoft.Extensions.Compliance.Redaction'

using System;
using System.Runtime.CompilerServices;

namespace Microsoft.Extensions.Compliance.Redaction;

/// <summary>
/// Redactor that replaces anything with an empty string.
/// </summary>
public sealed class ErasingRedactor : Redactor
{
    /// <summary>
    /// Gets the singleton instance of <see cref="T:Microsoft.Extensions.Compliance.Redaction.ErasingRedactor" />.
    /// </summary>
    public static ErasingRedactor Instance { get; }

    /// <inheritdoc />
    public override int Redact(ReadOnlySpan<char> source, Span<char> destination);

    /// <inheritdoc />
    public override int GetRedactedLength(ReadOnlySpan<char> input);

    public ErasingRedactor();
}
