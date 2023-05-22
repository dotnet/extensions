// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.EnumStrings;

namespace TestClasses
{
    [EnumStrings]
    public enum Negative0
    {
        MinusOne = -1
    }

    [EnumStrings]
    public enum Negative1
    {
        MinusOne = -1,
        MinusTwo = -2,
        MinusThree = -3,
        MinusFour = -4,
        MinusFive = -5,
        MinusSix = -6,
    }

    [EnumStrings]
    public enum NegativeLong0 : long
    {
        MinusOne = -1
    }

    [EnumStrings]
    public enum NegativeLong1 : long
    {
        MinusOne = -1,
        MinusTwo = -2,
        MinusThree = -3,
        MinusFour = -4,
        MinusFive = -5,
        MinusSix = -6,
    }
}
