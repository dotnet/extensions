// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using Microsoft.Extensions.Resilience.Options;
using Polly;
using Xunit;

namespace Microsoft.Extensions.Resilience.Polly.Test.Options;

public class HedgingTaskArgumentsTestsNonGeneric
{
    [Fact]
    public void Constructor_NoParameters_ShouldInitialize()
    {
        var instance = default(HedgingTaskArguments);

        Assert.Null(instance.Exception);
        Assert.Equal(0, instance.AttemptNumber);
        Assert.Null(instance.Context);
    }

    [Fact]
    public void Constructor_WithParameters_ShouldInitializeProperties()
    {
        var expectedError = "Something went wrong";
        var exception = new InvalidOperationException(expectedError);
        var expectedAttempts = 2;
        var context = new Context();
        var token = CancellationToken.None;

        var instance = new HedgingTaskArguments(
            exception,
            context,
            expectedAttempts,
            token);

        Assert.Equal(exception, instance.Exception);
        Assert.Equal(expectedAttempts, instance.AttemptNumber);
        Assert.Equal(context, instance.Context);
        Assert.Equal(token, instance.CancellationToken);
    }
}
