// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Options.Contextual;

namespace TestClasses
{
    [OptionsContext]
    public partial class Class2
    {
        public string Foo { get; } = "FooValue";
    }
}
