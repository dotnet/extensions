// Assembly 'Microsoft.Extensions.Compliance.Abstractions'

using System;
using System.Runtime.CompilerServices;

namespace Microsoft.Extensions.Compliance.Redaction;

/// <summary>
/// Enables the redaction of potentially sensitive data.
/// </summary>
public abstract class Redactor
{
    /// <summary>
    /// Redacts potentially sensitive data.
    /// </summary>
    /// <param name="source">Value to redact.</param>
    /// <returns>Redacted value.</returns>
    public string Redact(ReadOnlySpan<char> source);

    /// <summary>
    /// Redacts potentially sensitive data.
    /// </summary>
    /// <param name="source">Value to redact.</param>
    /// <param name="destination">Buffer to store redacted value.</param>
    /// <returns>Number of characters produced when redacting the given source input.</returns>
    /// <exception cref="T:System.ArgumentException"><paramref name="destination" /> is too small.</exception>
    public abstract int Redact(ReadOnlySpan<char> source, Span<char> destination);

    /// <summary>
    /// Redacts potentially sensitive data.
    /// </summary>
    /// <param name="source">Value to redact.</param>
    /// <param name="destination">Buffer to redact into.</param>
    /// <remarks>
    /// Returns 0 when <paramref name="source" /> is <see langword="null" />.
    /// </remarks>
    /// <returns>Number of characters written to the buffer.</returns>
    /// <exception cref="T:System.ArgumentException"><paramref name="destination" /> is too small.</exception>
    public int Redact(string? source, Span<char> destination);

    /// <summary>
    /// Redacts potentially sensitive data.
    /// </summary>
    /// <param name="source">Value to redact.</param>
    /// <returns>Redacted value.</returns>
    /// <remarks>
    /// Returns an empty string when <paramref name="source" /> is <see langword="null" />.
    /// </remarks>
    /// <exception cref="T:System.ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
    public virtual string Redact(string? source);

    /// <summary>
    /// Redacts potentially sensitive data.
    /// </summary>
    /// <typeparam name="T">Type of value to redact.</typeparam>
    /// <param name="value">Value to redact.</param>
    /// <param name="format">
    /// The optional format that selects the specific formatting operation performed. Refer to the
    /// documentation of the type being formatted to understand the values you can supply here.
    /// </param>
    /// <param name="provider">Format provider used to produce a string representing the value.</param>
    /// <returns>Redacted value.</returns>
    public string Redact<T>(T value, string? format = null, IFormatProvider? provider = null);

    /// <summary>
    /// Redacts potentially sensitive data.
    /// </summary>
    /// <typeparam name="T">Type of value to redact.</typeparam>
    /// <param name="value">Value to redact.</param>
    /// <param name="destination">Buffer to redact into.</param>
    /// <param name="format">
    /// The optional format string that selects the specific formatting operation performed. Refer to the
    /// documentation of the type being formatted to understand the values you can supply here.
    /// </param>
    /// <param name="provider">Format provider used to produce a string representing the value.</param>
    /// <returns>Number of characters written to the buffer.</returns>
    /// <exception cref="T:System.ArgumentException"><paramref name="destination" /> is too small.</exception>
    public int Redact<T>(T value, Span<char> destination, string? format = null, IFormatProvider? provider = null);

    /// <summary>
    /// Tries to redact potentially sensitive data.
    /// </summary>
    /// <typeparam name="T">The type of value to redact.</typeparam>
    /// <param name="value">The value to redact.</param>
    /// <param name="destination">The buffer to redact into.</param>
    /// <param name="charsWritten">When this method returns, contains the number of redacted characters that were written to the destination buffer.</param>
    /// <param name="format">
    /// The format string that selects the specific formatting operation performed. Refer to the
    /// documentation of the type being formatted to understand the values you can supply here.
    /// </param>
    /// <param name="provider">The format provider used to produce a string representing the value.</param>
    /// <returns><see langword="true" /> if the destination buffer was large enough, otherwise <see langword="false" />.</returns>
    public bool TryRedact<T>(T value, Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider = null);

    /// <summary>
    /// Gets the number of characters produced by redacting the input.
    /// </summary>
    /// <param name="input">Value to be redacted.</param>
    /// <returns>The number of characters produced by redacting the input.</returns>
    public abstract int GetRedactedLength(ReadOnlySpan<char> input);

    /// <summary>
    /// Gets the number of characters produced by redacting the input.
    /// </summary>
    /// <param name="input">Value to be redacted.</param>
    /// <returns>Minimum buffer size.</returns>
    public int GetRedactedLength(string? input);

    protected Redactor();
}
