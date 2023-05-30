// Assembly 'Microsoft.Extensions.Compliance.Testing'

using System;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Compliance.Redaction;
using Microsoft.Extensions.Options;
using Microsoft.Shared.Text;

namespace Microsoft.Extensions.Compliance.Testing;

public class FakeRedactor : Redactor
{
    public FakeRedactionCollector EventCollector { get; }
    public FakeRedactor(IOptions<FakeRedactorOptions>? options = null, FakeRedactionCollector? collector = null);
    public static FakeRedactor Create(FakeRedactorOptions? options = null, FakeRedactionCollector? collector = null);
    public override int Redact(ReadOnlySpan<char> source, Span<char> destination);
    public override int GetRedactedLength(ReadOnlySpan<char> input);
}
