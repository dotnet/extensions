// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using Microsoft.Extensions.Resilience.Options;
using Polly;
using Xunit;

namespace Microsoft.Extensions.Resilience.Polly.Test.Options;

public class FallbackTaskArgumentsTestsNonGeneric
{
    [Fact]
    public void Constructor_NoParameters_ShouldInitialize()
    {
#pragma warning disable SA1129
        var instance = new FallbackTaskArguments();

#pragma warning disable xUnit2002
        Assert.NotNull(instance);
    }

    [Fact]
    public void Constructor_WithParameters_ShouldInitializeProperties()
    {
        var expectedError = "Something went wrong";
        var exception = new InvalidOperationException(expectedError);
        var context = new Context();
        using var cts = new CancellationTokenSource();
        var ct = cts.Token;

        var instance = new FallbackTaskArguments(
            exception,
            context,
            ct);

        Assert.NotNull(instance);
        Assert.Equal(exception, instance.Exception);
        Assert.Equal(ct, instance.CancellationToken);
        Assert.Equal(context, instance.Context);
    }
}
