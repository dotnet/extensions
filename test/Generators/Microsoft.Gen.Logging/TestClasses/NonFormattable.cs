// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace TestClasses
{
    public readonly struct NonFormattable
    {
        public override string ToString() => "I refuse to be formatted";
    }
}
