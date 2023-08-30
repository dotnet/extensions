// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if NET8_0_OR_GREATER

using System.Collections.Generic;
using Xunit;

namespace Microsoft.AspNetCore.Telemetry.Http.Logging.Test;

public class IncomingHttpDimensionsTest
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
#endif
