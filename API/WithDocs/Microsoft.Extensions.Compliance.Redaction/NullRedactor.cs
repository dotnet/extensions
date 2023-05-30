// Assembly 'Microsoft.Extensions.Compliance.Redaction'

using System;
using System.Runtime.CompilerServices;

namespace Microsoft.Extensions.Compliance.Redaction;

/// <summary>
/// Redactor that does nothing to its input and returns it as-is.
/// </summary>
public sealed class NullRedactor : Redactor
{
    /// <summary>
    /// Gets the singleton instance of this class.
    /// </summary>
    public static NullRedactor Instance { get; }

    /// <inheritdoc />
    public override int GetRedactedLength(ReadOnlySpan<char> input);

    /// <inheritdoc />
    public override int Redact(ReadOnlySpan<char> source, Span<char> destination);

    /// <inheritdoc />
    public override string Redact(string? source);

    public NullRedactor();
}
