// Assembly 'Microsoft.Extensions.Compliance.Redaction'

using System;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Compliance.Redaction;

/// <summary>
/// Redactor that uses xxHash3 hashing to redact data.
/// </summary>
public sealed class XXHash3Redactor : Redactor
{
    /// <summary>
    /// Initializes a new instance of the <see cref="T:Microsoft.Extensions.Compliance.Redaction.XXHash3Redactor" /> class.
    /// </summary>
    /// <param name="options">The options to control the redactor.</param>
    public XXHash3Redactor(IOptions<XXHash3RedactorOptions> options);

    /// <inheritdoc />
    public override int GetRedactedLength(ReadOnlySpan<char> input);

    /// <inheritdoc />
    public override int Redact(ReadOnlySpan<char> source, Span<char> destination);
}
