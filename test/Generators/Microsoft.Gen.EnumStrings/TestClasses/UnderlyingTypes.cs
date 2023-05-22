// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.EnumStrings;

#pragma warning disable S4022
#pragma warning disable S2344
#pragma warning disable S1939

namespace TestClasses
{
    [EnumStrings]
    public enum SByteEnum1 : sbyte
    {
        One,
    }

    [EnumStrings]
    public enum SByteEnum2 : sbyte
    {
        One,
        Two,
        Three,
        Four
    }

    [EnumStrings]
    public enum SByteEnum3 : sbyte
    {
        One,
        Two,
        Three,
        Four = -42
    }

    [EnumStrings]
    public enum ByteEnum1 : byte
    {
        One,
    }

    [EnumStrings]
    public enum ByteEnum2 : byte
    {
        One,
        Two,
        Three,
        Four
    }

    [EnumStrings]
    public enum ByteEnum3 : byte
    {
        One,
        Two,
        Three,
        Four = 42
    }

    [EnumStrings]
    public enum ShortEnum1 : short
    {
        One,
    }

    [EnumStrings]
    public enum ShortEnum2 : short
    {
        One,
        Two,
        Three,
        Four
    }

    [EnumStrings]
    public enum ShortEnum3 : short
    {
        One,
        Two,
        Three,
        Four = -42
    }

    [EnumStrings]
    public enum UShortEnum1 : ushort
    {
        One,
    }

    [EnumStrings]
    public enum UShortEnum2 : ushort
    {
        One,
        Two,
        Three,
        Four
    }

    [EnumStrings]
    public enum UShortEnum3 : ushort
    {
        One,
        Two,
        Three,
        Four = 42
    }

    [EnumStrings]
    public enum IntEnum1 : int
    {
        One,
    }

    [EnumStrings]
    public enum IntEnum2 : int
    {
        One,
        Two,
        Three,
        Four
    }

    [EnumStrings]
    public enum IntEnum3 : int
    {
        One,
        Two,
        Three,
        Four = -42
    }

    [EnumStrings]
    public enum UIntEnum1 : uint
    {
        One,
    }

    [EnumStrings]
    public enum UIntEnum2 : uint
    {
        One,
        Two,
        Three,
        Four
    }

    [EnumStrings]
    public enum UIntEnum3 : uint
    {
        One,
        Two,
        Three,
        Four = 42
    }

    [EnumStrings]
    public enum LongEnum1 : long
    {
        One,
    }

    [EnumStrings]
    public enum LongEnum2 : long
    {
        One,
        Two,
        Three,
        Four
    }

    [EnumStrings]
    public enum LongEnum3 : long
    {
        One,
        Two,
        Three,
        Four = -42
    }

    [EnumStrings]
    public enum ULongEnum1 : ulong
    {
        One,
    }

    [EnumStrings]
    public enum ULongEnum2 : ulong
    {
        One,
        Two,
        Three,
        Four
    }

    [EnumStrings]
    public enum ULongEnum3 : ulong
    {
        One,
        Two,
        Three,
        Four = 42
    }
}
