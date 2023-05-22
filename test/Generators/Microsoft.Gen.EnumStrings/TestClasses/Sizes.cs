// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.EnumStrings;

namespace TestClasses
{
    [EnumStrings]
    public enum Size0
    {
    }

    [EnumStrings]
    public enum Size1
    {
        Zero,
    }

    [EnumStrings]
    public enum Size2
    {
        Zero,
        One,
    }

    [EnumStrings]
    public enum Size3
    {
        Zero,
        One,
        Two,
    }

    [EnumStrings]
    public enum Size4
    {
        Zero,
        One,
        Two,
        Three,
    }

    [EnumStrings]
    public enum Size5
    {
        Zero,
        One,
        Two,
        Three,
        Four,
    }

    [EnumStrings]
    public enum Size6
    {
        One = 1,
        Two = 2,
        Three = 3,
        Four = 4,
        Five = 5,
        Six = 6,
    }

    [EnumStrings]
    public enum Size7
    {
        One = 1,
        Two = 2,
        Three = 3,
        Four = 4,
        Five = 5,
        Six = 6,

        Ten = 10,
        Eleven = 11,
        Twelve = 12,
    }
}
