// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;

namespace TestClasses
{
    [OptionsContext]
    public partial class ClassWithNoAttribute
    {
        [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false, AllowMultiple = false)]
        public sealed class OptionsContextAttribute : Attribute
        {
        }

        public string Foo { get; } = "FooValue";
    }
}
