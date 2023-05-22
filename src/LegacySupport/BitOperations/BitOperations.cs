// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable S109 // Magic numbers should not be used

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System.Numerics;

[ExcludeFromCodeCoverage]
internal static class BitOperations
{
    // Summary:
    //     Rotates the specified value left by the specified number of bits.
    //
    // Parameters:
    //   value:
    //     The value to rotate.
    //
    //   offset:
    //     The number of bits to rotate by. Any value outside the range [0..31] is treated
    //     as congruent mod 32.
    //
    // Returns:
    //     The rotated value.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint RotateLeft(uint value, int offset)
        => (value << offset) | (value >> (32 - offset));

    // Summary:
    //     Rotates the specified value left by the specified number of bits.
    //
    // Parameters:
    //   value:
    //     The value to rotate.
    //
    //   offset:
    //     The number of bits to rotate by. Any value outside the range [0..63] is treated
    //     as congruent mod 64.
    //
    // Returns:
    //     The rotated value.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong RotateLeft(ulong value, int offset)
        => (value << offset) | (value >> (64 - offset));
}
