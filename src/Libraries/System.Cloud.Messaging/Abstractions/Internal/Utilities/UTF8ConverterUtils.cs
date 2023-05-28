// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text;

namespace System.Cloud.Messaging.Internal;

internal static class UTF8ConverterUtils
{
    /// <summary>
    /// Converts the <see cref="ReadOnlyMemory{T}"/> of <see cref="byte"/> to <see cref="string"/>.
    /// </summary>
    /// <remarks>
    /// Implementation copied from <see href="https://source.dot.net/#System.Memory.Data/System/BinaryData.cs,cab71e7b2240cb5c">public override unsafe string ToString()</see> method of BinaryData.
    /// No special treatment is given to the contents of the data, it is merely decoded as a UTF-8 string.
    /// For a JPEG or other binary file format the string will largely be nonsense with many embedded NUL characters,
    /// and UTF-8 JSON values will look like their file/network representation,
    /// including starting and stopping quotes on a string.
    /// </remarks>
    /// <param name="payload"><see cref="ReadOnlyMemory{T}"/> of <see cref="byte"/>.</param>
    /// <returns><see cref="string"/> value.</returns>
    public static unsafe string ConvertToUTF8StringUnsafe(ReadOnlyMemory<byte> payload)
    {
        ReadOnlySpan<byte> payloadSpan = payload.Span;
        if (payloadSpan.IsEmpty)
        {
            return string.Empty;
        }

        fixed (byte* ptr = payloadSpan)
        {
            return Encoding.UTF8.GetString(ptr, payloadSpan.Length);
        }
    }
}
