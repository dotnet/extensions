// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using Microsoft.Extensions.Resilience.Options;
using Polly;
using Xunit;

namespace Microsoft.Extensions.Resilience.Polly.Test.Options;

public class FallbackScenarioTaskArgumentsTests
{
    [Fact]
    public void Constructor_NoParameters_ShouldInitialize()
    {
#pragma warning disable SA1129
        var instance = new FallbackScenarioTaskArguments();

        Assert.Equal(CancellationToken.None, instance.CancellationToken);
        Assert.Null(instance.Context);
    }

    [Fact]
    public void Constructor_WithParameters_ShouldInitializeProperties()
    {
        var context = new Context();
        using var cts = new CancellationTokenSource();
        var ct = cts.Token;

        var instance = new FallbackScenarioTaskArguments(context, ct);

        Assert.Equal(ct, instance.CancellationToken);
        Assert.Equal(context, instance.Context);
    }
}
