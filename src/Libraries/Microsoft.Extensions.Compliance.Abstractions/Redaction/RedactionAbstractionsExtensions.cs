// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Text;

#if NETCOREAPP3_1_OR_GREATER
using Microsoft.Shared.Pools;
#endif

using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Compliance.Redaction;

/// <summary>
/// Redaction utility methods.
/// </summary>
public static class RedactionAbstractionsExtensions
{
    /// <summary>
    /// Redacts potentially sensitive data and appends it to a <see cref="StringBuilder"/> instance.
    /// </summary>
    /// <param name="stringBuilder">Instance of <see cref="StringBuilder"/> to append the redacted value.</param>
    /// <param name="redactor">The redactor that will redact the input value.</param>
    /// <param name="value">Value to redact.</param>
    /// <returns>Returns the value of <paramref name="stringBuilder"/>.</returns>
    /// <remarks>
    /// When the <paramref name="value"/> is <see langword="null"/> nothing will be appended to the string builder.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="stringBuilder"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="redactor"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    public static StringBuilder AppendRedacted(this StringBuilder stringBuilder, Redactor redactor, string? value)
        => AppendRedacted(stringBuilder, redactor, value.AsSpan());

    /// <summary>
    /// Redacts potentially sensitive data and appends it to a <see cref="StringBuilder"/> instance.
    /// </summary>
    /// <param name="stringBuilder">Instance of <see cref="StringBuilder"/> to append the redacted value.</param>
    /// <param name="redactor">The redactor that will redact the input value.</param>
    /// <param name="value">Value to redact.</param>
    /// <returns>Returns the value of <paramref name="stringBuilder"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="stringBuilder"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="redactor"/> is <see langword="null"/>.</exception>
    [SkipLocalsInit]
    public static StringBuilder AppendRedacted(this StringBuilder stringBuilder, Redactor redactor, ReadOnlySpan<char> value)
    {
        _ = Throw.IfNull(stringBuilder);
        _ = Throw.IfNull(redactor);

        if (value.IsEmpty)
        {
            return stringBuilder;
        }

#if NETCOREAPP3_1_OR_GREATER
        var length = redactor.GetRedactedLength(value);
        using var rental = new RentedSpan<char>(length);
        var destination = rental.Rented ? rental.Span : stackalloc char[length];

        var written = redactor.Redact(value, destination);
        return stringBuilder.Append(destination.Slice(0, written));
#else
        return stringBuilder.Append(redactor.Redact(value));
#endif
    }
}
