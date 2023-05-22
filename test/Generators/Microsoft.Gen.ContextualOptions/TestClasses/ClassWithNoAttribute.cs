// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;

namespace TestClasses
{
    [OptionsContext]
    [SuppressMessage("Minor Code Smell", "S2333:Redundant modifiers should not be used", Justification = "Needed for test code.")]
    public partial class ClassWithNoAttribute
    {
        [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false, AllowMultiple = false)]
        public sealed class OptionsContextAttribute : Attribute
        {
        }

        public string Foo { get; } = "FooValue";
    }
}
