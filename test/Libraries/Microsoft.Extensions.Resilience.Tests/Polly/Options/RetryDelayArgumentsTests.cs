// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using Microsoft.Extensions.Resilience.Options;
using Polly;
using Xunit;

namespace Microsoft.Extensions.Resilience.Polly.Test.Options;

public class RetryDelayArgumentsTests
{
    [Fact]
    public void Constructor_NoParameters_ShouldInitialize()
    {
#pragma warning disable SA1129
        var instance = new RetryDelayArguments<string>();

        Assert.Null(instance.Result);
        Assert.Null(instance.Context);
        Assert.Equal(CancellationToken.None, instance.CancellationToken);
    }

    [Fact]
    public void Constructor_WithParameters_ShouldInitializeProperties()
    {
        var expectedError = "Something went wrong";
        var delegateResult = new DelegateResult<string>(new InvalidOperationException(expectedError));
        var context = new Context();
        var cancellationToken = new CancellationToken();

        var instance = new RetryDelayArguments<string>(
            delegateResult,
            context,
            cancellationToken);

        Assert.Equal(delegateResult, instance.Result);
        Assert.Equal(context, instance.Context);
        Assert.Equal(cancellationToken, instance.CancellationToken);
    }
}
