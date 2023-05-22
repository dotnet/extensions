// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.EnumStrings;

namespace TestClasses
{
    [Flags]
    [EnumStrings]
    public enum Flags0
    {
    }

    [Flags]
    [EnumStrings]
    public enum Flags1
    {
        Zero = 1,
    }

    [Flags]
    [EnumStrings]
    public enum Flags2
    {
        Zero = 1,
        One = 2,
    }

    [Flags]
    [EnumStrings]
    public enum Flags3
    {
        Zero = 1,
        One = 2,
        Two = 4,
    }

    [Flags]
    [EnumStrings]
    public enum Flags4
    {
        Zero = 1,
        One = 2,
        Two = 4,
        Three = 8,
    }

    [Flags]
    [EnumStrings]
    public enum Flags5
    {
        Zero = 1,
        One = 2,
        Two = 4,
        Three = 8,
        Four = 16,
    }

    [Flags]
    [EnumStrings]
    public enum Flags6
    {
        Zero = 1,
        One = 2,
        Two = 4,
        Three = 8,
        Four = 16,
        Five = 32,
    }

    [Flags]
    [EnumStrings]
    public enum Flags7
    {
        Zero = 1,
        Two = 4,
        Three = 8,
    }

    [Flags]
    [EnumStrings]
    public enum Flags8
    {
        Zero = 1,
        Two = 4,
        Three = 8,
        Ten = 1024,
        Eleven = 2048,
    }

    [Flags]
    [EnumStrings]
    public enum Flags9 : ulong
    {
        Zero = 1,
        Two = 4,
        Three = 8,
        Ten = 1024,
        Eleven = 2048,
    }
}
