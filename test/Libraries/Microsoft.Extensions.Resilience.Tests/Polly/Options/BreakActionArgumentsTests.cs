// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using Microsoft.Extensions.Resilience.Options;
using Polly;
using Xunit;

namespace Microsoft.Extensions.Resilience.Polly.Test.Options;

public class BreakActionArgumentsTests
{
    [Fact]
    public void Constructor_NoParameters_ShouldInitialize()
    {
#pragma warning disable SA1129
        var instance = new BreakActionArguments<string>();

#pragma warning disable xUnit2002
        Assert.NotNull(instance);
    }

    [Fact]
    public void Constructor_With4Parameters_ShouldInitializeProperties()
    {
        var expectedError = "Something went wrong";
        var delegateResult = new DelegateResult<string>(new InvalidOperationException(expectedError));
        var expectedBreakDuration = TimeSpan.FromSeconds(2);
        var context = new Context();
        var cancellationToken = new CancellationToken();

        var instance = new BreakActionArguments<string>(
            delegateResult,
            context,
            expectedBreakDuration,
            cancellationToken);

        Assert.NotNull(instance);
        Assert.Equal(delegateResult, instance.Result);
        Assert.Equal(expectedBreakDuration, instance.BreakDuration);
        Assert.Equal(context, instance.Context);
        Assert.Equal(cancellationToken, instance.CancellationToken);
    }
}
