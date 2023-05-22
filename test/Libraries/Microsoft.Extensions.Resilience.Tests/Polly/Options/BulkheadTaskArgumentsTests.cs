// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using Microsoft.Extensions.Resilience.Options;
using Polly;
using Xunit;

namespace Microsoft.Extensions.Resilience.Polly.Test.Options;

public class BulkheadTaskArgumentsTests
{
    [Fact]
    public void Constructor_NoParameters_ShouldInitialize()
    {
#pragma warning disable SA1129
        var instance = new BulkheadTaskArguments();

#pragma warning disable xUnit2002
        Assert.NotNull(instance);
    }

    [Fact]
    public void Constructor_WithParameters_ShouldInitializeProperties()
    {
        var context = new Context();
        var instance = new BulkheadTaskArguments(
            context,
            CancellationToken.None);

        Assert.NotNull(instance);
        Assert.NotNull(instance.CancellationToken);
        Assert.Equal(context, instance.Context);
    }
}
