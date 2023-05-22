// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Microsoft.Extensions.EnumStrings.Tests;

public static class EnumStringsAttributeTests
{
    private enum Color
    {
        Red,
        Green,
        Blue,
    }

    [Fact]
    public static void All()
    {
        var a = new EnumStringsAttribute();
        Assert.Null(a.ExtensionNamespace);
        Assert.Null(a.ExtensionClassName);
        Assert.Equal("ToInvariantString", a.ExtensionMethodName);
        Assert.Equal("internal static", a.ExtensionClassModifiers);
        Assert.Null(a.EnumType);

        a = new EnumStringsAttribute(typeof(Color));
        Assert.Null(a.ExtensionNamespace);
        Assert.Null(a.ExtensionClassName);
        Assert.Equal("ToInvariantString", a.ExtensionMethodName);
        Assert.Equal("internal static", a.ExtensionClassModifiers);
        Assert.Equal(typeof(Color), a.EnumType);

        a.ExtensionNamespace = "A";
        a.ExtensionClassName = "B";
        a.ExtensionMethodName = "C";
        a.ExtensionClassModifiers = "D";

        Assert.Equal("A", a.ExtensionNamespace);
        Assert.Equal("B", a.ExtensionClassName);
        Assert.Equal("C", a.ExtensionMethodName);
        Assert.Equal("D", a.ExtensionClassModifiers);

    }
}
