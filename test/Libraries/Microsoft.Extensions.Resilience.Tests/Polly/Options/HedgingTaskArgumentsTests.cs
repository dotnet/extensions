// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using Microsoft.Extensions.Resilience.Options;
using Polly;
using Xunit;

namespace Microsoft.Extensions.Resilience.Polly.Test.Options;

public class HedgingTaskArgumentsTests
{
    [Fact]
    public void Constructor_NoParameters_ShouldInitialize()
    {
#pragma warning disable SA1129
        var instance = new HedgingTaskArguments<string>();

#pragma warning disable xUnit2002
        Assert.NotNull(instance);
    }

    [Fact]
    public void Constructor_WithParameters_ShouldInitializeProperties()
    {
        var expectedError = "Something went wrong";
        var delegateResult = new DelegateResult<string>(new InvalidOperationException(expectedError));
        var expectedAttempts = 2;
        var context = new Context();
        var token = new CancellationToken();

        var instance = new HedgingTaskArguments<string>(
            delegateResult,
            context,
            expectedAttempts,
            token);

        Assert.NotNull(instance);
        Assert.Equal(delegateResult, instance.Result);
        Assert.Equal(expectedAttempts, instance.AttemptNumber);
        Assert.Equal(context, instance.Context);
        Assert.Equal(token, instance.CancellationToken);
    }
}
