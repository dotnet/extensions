// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.Extensions.Primitives;
using Xunit;

namespace Microsoft.AspNetCore.HeaderParsing.Test;

public class HeaderParsingOptionsTests
{
    [Fact]
    public void ReadWrite()
    {
        var defValue = new Dictionary<string, StringValues>();
        var maxCachedValues = new Dictionary<string, int>();

        var options = new HeaderParsingOptions
        {
            DefaultValues = defValue,
            DefaultMaxCachedValuesPerHeader = 123,
            MaxCachedValuesPerHeader = maxCachedValues
        };

        Assert.Equal(defValue, options.DefaultValues);
        Assert.Equal(123, options.DefaultMaxCachedValuesPerHeader);
        Assert.Equal(maxCachedValues, options.MaxCachedValuesPerHeader);
    }
}
