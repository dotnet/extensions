// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable SA1310
#pragma warning disable S2148
#pragma warning disable S109

using System.Runtime.CompilerServices;

namespace System.IO.Hashing;

[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
internal static class XxHash64
{
    internal const ulong Prime64_1 = 0x9E3779B185EBCA87;
    internal const ulong Prime64_2 = 0xC2B2AE3D27D4EB4F;
    internal const ulong Prime64_3 = 0x165667B19E3779F9;
    internal const ulong Prime64_4 = 0x85EBCA77C2B2AE63;
    internal const ulong Prime64_5 = 0x27D4EB2F165667C5;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ulong Avalanche(ulong hash)
    {
        hash ^= hash >> 33;
        hash *= Prime64_2;
        hash ^= hash >> 29;
        hash *= Prime64_3;
        hash ^= hash >> 32;
        return hash;
    }
}
