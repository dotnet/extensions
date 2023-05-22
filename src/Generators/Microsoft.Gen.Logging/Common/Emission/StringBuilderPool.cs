// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Text;

namespace Microsoft.Gen.Logging.Emission;

internal sealed class StringBuilderPool
{
    private readonly Stack<StringBuilder> _builders = new();

    public StringBuilder GetStringBuilder()
    {
        const int DefaultStringBuilderCapacity = 1024;

        if (_builders.Count == 0)
        {
            return new StringBuilder(DefaultStringBuilderCapacity);
        }

        var sb = _builders.Pop();
        _ = sb.Clear();
        return sb;
    }

    public void ReturnStringBuilder(StringBuilder sb)
    {
        _builders.Push(sb);
    }
}
