// Assembly 'Microsoft.Extensions.Compliance.Abstractions'

using System;
using System.Runtime.CompilerServices;
using System.Text;

namespace Microsoft.Extensions.Compliance.Redaction;

public static class RedactionAbstractionsExtensions
{
    public static StringBuilder AppendRedacted(this StringBuilder stringBuilder, Redactor redactor, string? value);
    public static StringBuilder AppendRedacted(this StringBuilder stringBuilder, Redactor redactor, ReadOnlySpan<char> value);
}
