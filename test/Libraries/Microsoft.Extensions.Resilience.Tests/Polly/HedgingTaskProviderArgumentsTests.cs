// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using Polly;
using Xunit;

namespace Microsoft.Extensions.Resilience.Polly.Test;

public class HedgingTaskProviderArgumentsTests
{
    [Fact]
    public void Constructor_NoParameters_ShouldInitialize()
    {
#pragma warning disable SA1129 // Do not use default value type constructor
        var instance = new HedgingTaskProviderArguments();
#pragma warning restore SA1129 // Do not use default value type constructor

        Assert.Null(instance.Context);
        Assert.Equal(0, instance.AttemptNumber);
        Assert.Equal(CancellationToken.None, instance.CancellationToken);
    }

    [Fact]
    public void Constructor_WithParameters_ShouldInitializeProperties()
    {
        var context = new Context();
#pragma warning disable SA1129 // Do not use default value type constructor
        var cancellationToken = new CancellationToken();
#pragma warning restore SA1129 // Do not use default value type constructor
        var instance = new HedgingTaskProviderArguments(
            context,
            2,
            CancellationToken.None);

        Assert.Equal(context, instance.Context);
        Assert.Equal(2, instance.AttemptNumber);
        Assert.Equal(cancellationToken, instance.CancellationToken);
    }
}
