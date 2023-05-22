// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace System.Text;

[ExcludeFromCodeCoverage]
internal static class StringBuilderExtensions
{
    public static StringBuilder Append(this StringBuilder sb, ReadOnlySpan<char> value)
    {
        if (value.Length > 0)
        {
            unsafe
            {
                fixed (char* valueChars = &MemoryMarshal.GetReference(value))
                {
                    _ = sb.Append(valueChars, value.Length);
                }
            }
        }

        return sb;
    }
}
