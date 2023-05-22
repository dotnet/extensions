// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.EnumStrings;

#pragma warning disable CA1069
#pragma warning disable CA1027

namespace TestClasses
{
    [EnumStrings]
    public enum Overlapping1
    {
        Zero,
        One,
        Un = One,
        Two,
        Deux = Two,
        Three,
        Four,
    }

    [Flags]
    [EnumStrings]
    public enum Overlapping2
    {
        None = 0,
        One = 1,
        Two = 2,
        Deux = 2,
        Four = 4,
        Eight = 8,
        Twelve = 12,
        Douze = 12,
        Thirteen = 13,
    }
}
