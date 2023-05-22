// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using Microsoft.Extensions.Resilience.Options;
using Polly;
using Xunit;

namespace Microsoft.Extensions.Resilience.Polly.Test.Options;

public class RetryActionArgumentsTestsNonGeneric
{
    [Fact]
    public void Constructor_NoParameters_ShouldInitialize()
    {
#pragma warning disable SA1129
        var instance = new RetryActionArguments();

#pragma warning disable xUnit2002
        Assert.NotNull(instance);
    }

    [Fact]
    public void Constructor_With5Parameters_ShouldInitializeProperties()
    {
        var expectedError = "Something went wrong";
        var exception = new InvalidOperationException(expectedError);
        var expectedWaitingInterval = TimeSpan.FromSeconds(2);
        var expectedAttempts = 2;
        var context = new Context();
        var cancellationToken = new CancellationToken();

        var instance = new RetryActionArguments(
            exception,
            context,
            expectedWaitingInterval,
            expectedAttempts,
            cancellationToken);

        Assert.NotNull(instance);
        Assert.Equal(exception, instance.Exception);
        Assert.Equal(expectedWaitingInterval, instance.WaitingTimeInterval);
        Assert.Equal(expectedAttempts, instance.AttemptNumber);
        Assert.Equal(context, instance.Context);
        Assert.Equal(cancellationToken, instance.CancellationToken);
    }
}
