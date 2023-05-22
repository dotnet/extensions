// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Xunit;

namespace Microsoft.Shared.Text.Test;

public static class NumericExtensionsTests
{
    [Fact]
    public static void Int()
    {
        for (int i = -2000; i < 2000; i++)
        {
            var expected = i.ToString(CultureInfo.InvariantCulture);
            var actual = i.ToInvariantString();
            Assert.Equal(expected, actual);
        }
    }

    [Fact]
    public static void Long()
    {
        for (long i = -2000; i < 2000; i++)
        {
            var expected = i.ToString(CultureInfo.InvariantCulture);
            var actual = i.ToInvariantString();
            Assert.Equal(expected, actual);
        }
    }
}
