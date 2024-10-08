// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>Provides internal helpers for implementing caching services.</summary>
internal static class CachingHelpers
{
    /// <summary>Computes a default cache key for the specified parameters.</summary>
    /// <typeparam name="TValue">Specifies the type of the data being used to compute the key.</typeparam>
    /// <param name="value">The data with which to compute the key.</param>
    /// <param name="serializerOptions">The <see cref="JsonSerializerOptions"/>.</param>
    /// <returns>A string that will be used as a cache key.</returns>
    public static string GetCacheKey<TValue>(TValue value, JsonSerializerOptions serializerOptions)
        => GetCacheKey(value, false, serializerOptions);

    /// <summary>Computes a default cache key for the specified parameters.</summary>
    /// <typeparam name="TValue">Specifies the type of the data being used to compute the key.</typeparam>
    /// <param name="value">The data with which to compute the key.</param>
    /// <param name="flag">Another data item that causes the key to vary.</param>
    /// <param name="serializerOptions">The <see cref="JsonSerializerOptions"/>.</param>
    /// <returns>A string that will be used as a cache key.</returns>
    public static string GetCacheKey<TValue>(TValue value, bool flag, JsonSerializerOptions serializerOptions)
    {
        _ = Throw.IfNull(value);
        _ = Throw.IfNull(serializerOptions);
        serializerOptions.MakeReadOnly();

        var jsonKeyBytes = JsonSerializer.SerializeToUtf8Bytes(value, serializerOptions.GetTypeInfo(typeof(TValue)));

        if (flag && jsonKeyBytes.Length > 0)
        {
            // Make an arbitrary change to the hash input based on the flag
            // The alternative would be including the flag in "value" in the
            // first place, but that's likely to require an extra allocation
            // or the inclusion of another type in the JsonSerializerContext.
            // This is a micro-optimization we can change at any time.
            jsonKeyBytes[0] = (byte)(byte.MaxValue - jsonKeyBytes[0]);
        }

        // The complete JSON representation is excessively long for a cache key, duplicating much of the content
        // from the value. So we use a hash of it as the default key.
#if NET8_0_OR_GREATER
        Span<byte> hashData = stackalloc byte[SHA256.HashSizeInBytes];
        SHA256.HashData(jsonKeyBytes, hashData);
        return Convert.ToHexString(hashData);
#else
        using var sha256 = SHA256.Create();
        var hashData = sha256.ComputeHash(jsonKeyBytes);
        return BitConverter.ToString(hashData).Replace("-", string.Empty);
#endif
    }
}
