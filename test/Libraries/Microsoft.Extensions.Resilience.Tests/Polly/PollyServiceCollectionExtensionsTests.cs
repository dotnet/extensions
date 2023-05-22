// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Telemetry;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Resilience.Internal;
using Microsoft.Extensions.Telemetry.Metering;
using Moq;
using Xunit;

namespace Microsoft.Extensions.Resilience.Polly.Test;

#pragma warning disable CS0618
public class PollyServiceCollectionExtensionsTests
{
    private readonly IServiceCollection _serviceCollection;

    public PollyServiceCollectionExtensionsTests()
    {
        _serviceCollection = new ServiceCollection();
        _serviceCollection.RegisterMetering().AddLogging().AddSingleton(Mock.Of<IOutgoingRequestContext>());
    }

    [Fact]
    public void ConfigureFailureResultContext()
    {
        var expectedDimensionValue = "lalala";
        _ = _serviceCollection
            .ConfigureFailureResultContext<string>((_) => FailureResultContext.Create(failureReason: expectedDimensionValue));

        using var serviceProvider = _serviceCollection.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptions<FailureEventMetricsOptions<string>>>()?.Value;
        Assert.NotNull(options);

        var context = options!.GetContextFromResult(string.Empty);

        Assert.Equal("unknown", context.FailureSource);
        Assert.Equal(expectedDimensionValue, context.FailureReason);
        Assert.Equal("unknown", context.AdditionalInformation);
    }

    [Fact]
    public void ConfigureFailureResultContext_ArgumentValidation()
    {
        Assert.Throws<ArgumentNullException>(() => _serviceCollection.ConfigureFailureResultContext<string>(null!));
        Assert.Throws<ArgumentNullException>(() => PollyServiceCollectionExtensions.ConfigureFailureResultContext<string>(null!, args => default));
    }

    [Fact]
    public void ShouldThrowArgumentExceptionWhenFailureSourceIsNullOrEmpty()
    {
        string s = "lalala";

        ShouldThrowArgumentException((_) => FailureResultContext.Create(null!, s, s));
        ShouldThrowArgumentException((_) => FailureResultContext.Create(s, null!, s));
        ShouldThrowArgumentException((_) => FailureResultContext.Create(s, s, null!));

        ShouldThrowArgumentException((_) => FailureResultContext.Create("", s, s), false);
        ShouldThrowArgumentException((_) => FailureResultContext.Create(s, "", s), false);
        ShouldThrowArgumentException((_) => FailureResultContext.Create(s, s, ""), false);
    }

    private void ShouldThrowArgumentException(Func<string, FailureResultContext> testCode, bool isNull = true)
    {
        _ = _serviceCollection
            .ConfigureFailureResultContext<string>(testCode);

        using var serviceProvider = _serviceCollection.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptions<FailureEventMetricsOptions<string>>>()?.Value;
        Assert.NotNull(options);

        if (isNull)
        {
            Assert.Throws<ArgumentNullException>(() => options!.GetContextFromResult(string.Empty));
        }
        else
        {
            Assert.Throws<ArgumentException>(() => options!.GetContextFromResult(string.Empty));
        }
    }
}
