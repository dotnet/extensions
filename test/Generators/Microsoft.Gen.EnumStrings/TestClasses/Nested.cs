// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.EnumStrings;

namespace TestClasses
{
    public static class Nested
    {
        [EnumStrings]
        public enum Fruit
        {
            Banana,
            Apple,
            Peach,
        }
    }
}
