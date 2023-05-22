// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Globalization;
using System.IO.Hashing;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Options;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Compliance.Redaction;

/// <summary>
/// Redactor that uses xxHash3 hashing to redact data.
/// </summary>
public sealed class XXHash3Redactor : Redactor
{
    internal const int HashSize = 16;
    internal const string Prefix = "<xxhash:";
    internal const string Postfix = ">";
    internal static readonly int RedactedSize = HashSize + Prefix.Length + Postfix.Length;

    private readonly ulong _seed;

    /// <summary>
    /// Initializes a new instance of the <see cref="XXHash3Redactor"/> class.
    /// </summary>
    /// <param name="options">The options to control the redactor.</param>
    public XXHash3Redactor(IOptions<XXHash3RedactorOptions> options)
    {
        _seed = Throw.IfMemberNull(options, options?.Value).HashSeed;
    }

    /// <inheritdoc />
    public override int GetRedactedLength(ReadOnlySpan<char> input)
    {
        if (input.IsEmpty)
        {
            return 0;
        }

        return RedactedSize;
    }

    /// <inheritdoc />
    public override int Redact(ReadOnlySpan<char> source, Span<char> destination)
    {
        var length = GetRedactedLength(source);

        if (length == 0)
        {
            return 0;
        }

        Throw.IfBufferTooSmall(destination.Length, length, nameof(destination));

        var s = MemoryMarshal.AsBytes(source);
        var hash = XxHash3.HashToUInt64(s, (long)_seed);

#pragma warning disable S109 // Magic numbers should not be used
        destination[24] = '>'; // do this first to avoid redundant bounds checking
        destination[0] = '<';
        destination[1] = 'x';
        destination[2] = 'x';
        destination[3] = 'h';
        destination[4] = 'a';
        destination[5] = 's';
        destination[6] = 'h';
        destination[7] = ':';
#pragma warning restore S109 // Magic numbers should not be used

#if NETCOREAPP3_1_OR_GREATER
        _ = hash.TryFormat(destination.Slice(Prefix.Length), out var _, "x", CultureInfo.InvariantCulture);
#else
        var str = hash.ToString("x", CultureInfo.InvariantCulture);
        for (int i = 0; i < str.Length; i++)
        {
            destination[Prefix.Length + i] = str[i];
        }
#endif

        return RedactedSize;
    }
}
