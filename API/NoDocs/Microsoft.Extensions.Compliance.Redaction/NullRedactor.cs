// Assembly 'Microsoft.Extensions.Compliance.Redaction'

using System;
using System.Runtime.CompilerServices;

namespace Microsoft.Extensions.Compliance.Redaction;

public sealed class NullRedactor : Redactor
{
    public static NullRedactor Instance { get; }
    public override int GetRedactedLength(ReadOnlySpan<char> input);
    public override int Redact(ReadOnlySpan<char> source, Span<char> destination);
    public override string Redact(string? source);
    public NullRedactor();
}
