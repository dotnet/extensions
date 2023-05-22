// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.EnumStrings;

[assembly: EnumStrings(typeof(TestClasses.Level))]
[assembly: EnumStrings(typeof(TestClasses.Medal))]

namespace TestClasses
{
    public enum Level
    {
        One,
        Two,
        Three,
    }

    public enum Medal
    {
        Bronze,
        Silver,
        Gold,
    }
}
