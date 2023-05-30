// Assembly 'Microsoft.Extensions.Compliance.Abstractions'

using System;
using System.Runtime.CompilerServices;

namespace Microsoft.Extensions.Compliance.Redaction;

public abstract class Redactor
{
    public string Redact(ReadOnlySpan<char> source);
    public abstract int Redact(ReadOnlySpan<char> source, Span<char> destination);
    public int Redact(string? source, Span<char> destination);
    public virtual string Redact(string? source);
    public string Redact<T>(T value, string? format = null, IFormatProvider? provider = null);
    public int Redact<T>(T value, Span<char> destination, string? format = null, IFormatProvider? provider = null);
    public abstract int GetRedactedLength(ReadOnlySpan<char> input);
    public int GetRedactedLength(string? input);
    protected Redactor();
}
