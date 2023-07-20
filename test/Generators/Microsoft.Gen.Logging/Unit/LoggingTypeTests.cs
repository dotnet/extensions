// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Gen.Logging.Model;
using Xunit;

namespace Microsoft.Gen.Logging.Test;

public class LoggingTypeTests
{
    [Fact]
    public void Fields_Should_BeInitialized()
    {
        var instance = new LoggingType();
        Assert.Empty(instance.Name);
        Assert.Empty(instance.Namespace);
        Assert.Empty(instance.Keyword);
    }
}
