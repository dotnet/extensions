// Assembly 'Microsoft.Extensions.Compliance.Abstractions'

using System;
using System.Runtime.CompilerServices;
using System.Text;

namespace Microsoft.Extensions.Compliance.Redaction;

/// <summary>
/// Redaction utility methods.
/// </summary>
public static class RedactionAbstractionsExtensions
{
    /// <summary>
    /// Redacts potentially sensitive data and appends it to a <see cref="T:System.Text.StringBuilder" /> instance.
    /// </summary>
    /// <param name="stringBuilder">Instance of <see cref="T:System.Text.StringBuilder" /> to append the redacted value.</param>
    /// <param name="redactor">The redactor that will redact the input value.</param>
    /// <param name="value">Value to redact.</param>
    /// <returns>Returns the value of <paramref name="stringBuilder" />.</returns>
    /// <remarks>
    /// When the <paramref name="value" /> is <see langword="null" /> nothing will be appended to the string builder.
    /// </remarks>
    /// <exception cref="T:System.ArgumentNullException">When <paramref name="stringBuilder" /> is <see langword="null" />.</exception>
    /// <exception cref="T:System.ArgumentNullException">When <paramref name="redactor" /> is <see langword="null" />.</exception>
    /// <exception cref="T:System.ArgumentNullException">When <paramref name="value" /> is <see langword="null" />.</exception>
    public static StringBuilder AppendRedacted(this StringBuilder stringBuilder, Redactor redactor, string? value);

    /// <summary>
    /// Redacts potentially sensitive data and appends it to a <see cref="T:System.Text.StringBuilder" /> instance.
    /// </summary>
    /// <param name="stringBuilder">Instance of <see cref="T:System.Text.StringBuilder" /> to append the redacted value.</param>
    /// <param name="redactor">The redactor that will redact the input value.</param>
    /// <param name="value">Value to redact.</param>
    /// <returns>Returns the value of <paramref name="stringBuilder" />.</returns>
    /// <exception cref="T:System.ArgumentNullException">When <paramref name="stringBuilder" /> is <see langword="null" />.</exception>
    /// <exception cref="T:System.ArgumentNullException">When <paramref name="redactor" /> is <see langword="null" />.</exception>
    public static StringBuilder AppendRedacted(this StringBuilder stringBuilder, Redactor redactor, ReadOnlySpan<char> value);
}
