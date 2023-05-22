// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.EnumStrings;

[assembly: EnumStrings(typeof(TestClasses.Options1), ExtensionNamespace = "NamespaceA", ExtensionClassName = "ClassB", ExtensionMethodName = "MethodC")]

namespace TestClasses
{
    [EnumStrings(ExtensionNamespace = "NamespaceX", ExtensionClassName = "ClassY", ExtensionMethodName = "MethodZ", ExtensionClassModifiers = "public static")]
    public enum Options0
    {
        Option0,
    }

    public enum Options1
    {
        Option0,
        Option1,
    }
}
