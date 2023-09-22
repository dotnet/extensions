// Assembly 'Microsoft.Extensions.Compliance.Redaction'

using System;
using System.Runtime.CompilerServices;

namespace Microsoft.Extensions.Compliance.Redaction;

public sealed class ErasingRedactor : Redactor
{
    public static ErasingRedactor Instance { get; }
    public override int Redact(ReadOnlySpan<char> source, Span<char> destination);
    public override int GetRedactedLength(ReadOnlySpan<char> input);
    public ErasingRedactor();
}
