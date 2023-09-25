﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace Microsoft.AspNetCore.Diagnostics.Logging.Test;

public class IncomingHttpDimensionsTests
{
    [Fact]
    public void Should_ReturnList_AllDimensions()
    {
        var dimensions = HttpLoggingTagNames.DimensionNames;
        Assert.Equal(9, dimensions.Count);

        var names = new HashSet<string>(dimensions);
        Assert.Equal(names.Count, dimensions.Count);
    }
}
