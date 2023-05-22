// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using Microsoft.Extensions.Resilience.Options;
using Polly;
using Xunit;

namespace Microsoft.Extensions.Resilience.Polly.Test.Options;

public class TimeoutTaskArgumentsTests
{
    [Fact]
    public void Constructor_NoParameters_ShouldInitialize()
    {
#pragma warning disable SA1129
        var instance = new TimeoutTaskArguments();

        Assert.Null(instance.Context);
        Assert.Equal(CancellationToken.None, instance.CancellationToken);
    }

    [Fact]
    public void Constructor_WithParameters_ShouldInitializeProperties()
    {
        var context = new Context();
        var instance = new TimeoutTaskArguments(context, CancellationToken.None);

        Assert.Equal(CancellationToken.None, instance.CancellationToken);
        Assert.Equal(context, instance.Context);
    }
}
