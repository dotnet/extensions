// Assembly 'Microsoft.Extensions.Compliance.Abstractions'

using System.Runtime.CompilerServices;
using Microsoft.Extensions.Compliance.Redaction;

namespace System.Text;

public static class RedactionStringBuilderExtensions
{
    public static StringBuilder AppendRedacted(this StringBuilder stringBuilder, Redactor redactor, string? value);
    public static StringBuilder AppendRedacted(this StringBuilder stringBuilder, Redactor redactor, ReadOnlySpan<char> value);
}
