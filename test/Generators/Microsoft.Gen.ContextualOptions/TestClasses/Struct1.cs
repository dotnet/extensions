﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Options.Contextual;

namespace TestClasses
{
    [OptionsContext]
    public partial struct Struct1
    {
#pragma warning disable CA1822 // Mark members as static
        public readonly string Foo => "FooValue";
    }
}
