// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.Extensions.Diagnostics.Buffering;

internal sealed class StringifyComprarer : IEqualityComparer<KeyValuePair<string, object?>>
{
    public bool Equals(KeyValuePair<string, object?> x, KeyValuePair<string, object?> y)
    {
        if (x.Key != y.Key)
        {
            return false;
        }

        if (x.Value is null && y.Value is null)
        {
            return true;
        }

        if (x.Value is null || y.Value is null)
        {
            return false;
        }

        return x.Value.ToString() == y.Value.ToString();
    }

    public int GetHashCode(KeyValuePair<string, object?> obj)
    {
        return HashCode.Combine(obj.Key, obj.Value?.ToString());
    }
}
