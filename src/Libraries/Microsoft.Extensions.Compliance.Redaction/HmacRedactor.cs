// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using Microsoft.Shared.Diagnostics;
using Microsoft.Shared.Text;

#if NET6_0_OR_GREATER
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
#else
using System.Diagnostics.CodeAnalysis;
using System.Text;
#endif

namespace Microsoft.Extensions.Compliance.Redaction;

internal sealed class HmacRedactor : Redactor
{
#if NET6_0_OR_GREATER
    private const int SHA256HashSizeInBytes = 32;
#endif
    private const int BytesOfHashWeUse = 16;

    /// <remarks>
    /// Magic numbers are formula for calculating base64 length with padding.
    /// </remarks>
    private const int Base64HashLength = ((BytesOfHashWeUse + 2) / 3) * 4;

    private readonly int _redactedLength;
    private readonly byte[] _hashKey;
    private readonly string _keyId;

    public HmacRedactor(IOptions<HmacRedactorOptions> options)
    {
        var value = Throw.IfMemberNull(options, options.Value);

        _hashKey = Convert.FromBase64String(value.Key);
        _keyId = value.KeyId.HasValue ? value.KeyId.Value.ToInvariantString() + ':' : string.Empty;
        _redactedLength = Base64HashLength + _keyId.Length;
    }

    public override int GetRedactedLength(ReadOnlySpan<char> source)
    {
        if (source.IsEmpty)
        {
            return 0;
        }

        return _redactedLength;
    }

#if NET6_0_OR_GREATER
    public override int Redact(ReadOnlySpan<char> source, Span<char> destination)
    {
        var length = GetRedactedLength(source);
        if (length == 0)
        {
            return 0;
        }

        Throw.IfBufferTooSmall(destination.Length, length, nameof(destination));

        _keyId.AsSpan().CopyTo(destination);
        return CreateSha256Hash(source, destination[_keyId.Length..], _hashKey) + _keyId.Length;
    }

    [SkipLocalsInit]
    private static int CreateSha256Hash(ReadOnlySpan<char> source, Span<char> destination, byte[] hashKey)
    {
        Span<byte> hashBuffer = stackalloc byte[SHA256HashSizeInBytes];

        _ = HMACSHA256.HashData(hashKey, MemoryMarshal.AsBytes(source), hashBuffer);

        // this won't fail, we ensured the destination is big enough previously
        _ = Convert.TryToBase64Chars(hashBuffer.Slice(0, BytesOfHashWeUse), destination, out int charsWritten);

        return charsWritten;
    }

#else

    public override int Redact(ReadOnlySpan<char> source, Span<char> destination)
    {
        const int RemainingBytesToPadForBase64Hash = BytesOfHashWeUse % 3;

        var length = GetRedactedLength(source);
        if (length == 0)
        {
            return 0;
        }

        Throw.IfBufferTooSmall(destination.Length, length, nameof(destination));

        _keyId.AsSpan().CopyTo(destination);
        return ConvertBytesToBase64(CreateSha256Hash(source, _hashKey), destination, RemainingBytesToPadForBase64Hash, _keyId.Length) + _keyId.Length;
    }

    private static byte[] CreateSha256Hash(ReadOnlySpan<char> value, byte[] hashKey)
    {
        using var hmac = new HMACSHA256(hashKey);
        return hmac.ComputeHash(Encoding.Unicode.GetBytes(value.ToArray()));
    }

    private static readonly char[] _base64CharactersTable =
    {
        'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O',
        'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z', 'a', 'b', 'c', 'd',
        'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's',
        't', 'u', 'v', 'w', 'x', 'y', 'z', '0', '1', '2', '3', '4', '5', '6', '7',
        '8', '9', '+', '/', '=',
    };

    [SuppressMessage("Code smell", "S109", Justification = "Bit operation.")]
    private static int ConvertBytesToBase64(byte[] hashToConvert, Span<char> destination, int remainingBytesToPad, int startOffset)
    {
        var iterations = BytesOfHashWeUse - remainingBytesToPad;
        var offset = startOffset;

        unchecked
        {
            for (var i = 0; i < iterations; i += 3)
            {
                destination[offset] = _base64CharactersTable[(hashToConvert[i] & 0xfc) >> 2];
                destination[offset + 1] = _base64CharactersTable[((hashToConvert[i] & 0x03) << 4) | ((hashToConvert[i + 1] & 0xf0) >> 4)];
                destination[offset + 2] = _base64CharactersTable[((hashToConvert[i + 1] & 0x0f) << 2) | ((hashToConvert[i + 2] & 0xc0) >> 6)];
                destination[offset + 3] = _base64CharactersTable[hashToConvert[i + 2] & 0x3f];
                offset += 4;
            }

#if false
// this code is disabled since it is never visited given the limited use of this function. We leave it here in case the code is needed in the future
            if (remainingBytesToPad == 2)
            {
                destination[offset] = _base64CharactersTable[(hashToConvert[iterations] & 0xfc) >> 2];
                destination[offset + 1] = _base64CharactersTable[((hashToConvert[iterations] & 0x03) << 4) | ((hashToConvert[iterations + 1] & 0xf0) >> 4)];
                destination[offset + 2] = _base64CharactersTable[(hashToConvert[iterations + 1] & 0x0f) << 2];
                destination[offset + 3] = _base64CharactersTable[64];
                offset += 4;
            }
#endif

            if (remainingBytesToPad == 1)
            {
                destination[offset] = _base64CharactersTable[(hashToConvert[iterations] & 0xfc) >> 2];
                destination[offset + 1] = _base64CharactersTable[(hashToConvert[iterations] & 0x03) << 4];
                destination[offset + 2] = _base64CharactersTable[64];
                destination[offset + 3] = _base64CharactersTable[64];
                offset += 4;
            }
        }

        var charsWritten = offset - startOffset;
        return charsWritten;
    }

#endif
}
