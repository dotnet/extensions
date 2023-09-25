// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring;

internal sealed class ByteArrayEqualityComparer : IEqualityComparer<byte[]>
{
    public bool Equals(byte[]? x, byte[]? y)
    {
        if (x == null || y == null)
        {
            return x == y;
        }

        return x.AsSpan().SequenceEqual(y);
    }

    public int GetHashCode(byte[] x)
    {
        var hash = default(HashCode);
#if NET6_0_OR_GREATER
        hash.AddBytes(x);
#else
        foreach (var obj in x)
        {
            hash.Add(obj);
        }
#endif
        return hash.ToHashCode();
    }
}

