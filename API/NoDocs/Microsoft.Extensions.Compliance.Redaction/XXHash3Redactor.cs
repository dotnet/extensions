// Assembly 'Microsoft.Extensions.Compliance.Redaction'

using System;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Compliance.Redaction;

public sealed class XXHash3Redactor : Redactor
{
    public XXHash3Redactor(IOptions<XXHash3RedactorOptions> options);
    public override int GetRedactedLength(ReadOnlySpan<char> input);
    public override int Redact(ReadOnlySpan<char> source, Span<char> destination);
}
