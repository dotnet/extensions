// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Forked from StyleCop.Analyzers repo.

using System;
using Xunit;

namespace Microsoft.Extensions.LocalAnalyzers.Json.Test;

public class JsonParseExceptionTest
{
    [Fact]
    public void TestDefaultConstructor()
    {
        var ex = new JsonParseException();
        Assert.Equal(ParsingError.Unknown, ex.Error);
        Assert.False(string.IsNullOrEmpty(ex.Message));
    }
}
