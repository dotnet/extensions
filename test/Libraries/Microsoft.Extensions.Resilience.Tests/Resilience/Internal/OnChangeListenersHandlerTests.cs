// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Resilience.Internal;
using Microsoft.Extensions.Resilience.Options;
using Moq;
using Xunit;

namespace Microsoft.Extensions.Resilience.Internal.Test;
public class OnChangeListenersHandlerTests
{
    [Fact]
    public void TryCaptureOnChange_DistinctNamesAndSameType_ShouldAdd()
    {
        var name1 = "options1";
        var name2 = "options2";
        using var handler = GetHandler();

        Assert.True(handler.TryCaptureOnChange<TimeoutPolicyOptions>(name1));
        Assert.True(handler.TryCaptureOnChange<TimeoutPolicyOptions>(name2));
    }

    [Fact]
    public void TryCaptureOnChange_SameNameAndType_ShouldAddOnlyOnce()
    {
        var name1 = "options1";
        using var handler = GetHandler();

        Assert.True(handler.TryCaptureOnChange<TimeoutPolicyOptions>(name1));
        Assert.False(handler.TryCaptureOnChange<TimeoutPolicyOptions>(name1));
        Assert.False(handler.TryCaptureOnChange<TimeoutPolicyOptions>(name1));
    }

    [Theory]
    [InlineData("name", "name")]
    [InlineData("name1", "name2")]
    public void TryCaptureOnChange_DistinctTypes_ShouldAdd(string name1, string name2)
    {
        using var handler = GetHandler();
        Assert.True(handler.TryCaptureOnChange<TimeoutPolicyOptions>(name1));
        Assert.True(handler.TryCaptureOnChange<RetryPolicyOptions>(name2));
    }

    [Fact]
    public void TryCaptureOnChange_WhenNullListenerReturned_ShouldNotAdd()
    {
        var name = "dummyPipeline";
        var disposeCalls = 0;
        var listenerMock = new Mock<IDisposable>();
        var optionsMonitorMock = new Mock<IOptionsMonitor<TimeoutPolicyOptions>>(MockBehavior.Strict);
        var options = new TimeoutPolicyOptions();
        listenerMock.Setup(mock => mock.Dispose()).Callback(() => { disposeCalls++; });
        optionsMonitorMock
            .Setup(mock => mock.Get(name))
            .Returns(options);
        optionsMonitorMock
            .SetupSequence(mock => mock.OnChange(It.IsAny<Action<TimeoutPolicyOptions, string?>>()))
            .Returns((IDisposable)null!)
            .Returns(listenerMock.Object);

        using var handler = GetHandler(services =>
            services.AddSingleton(optionsMonitorMock.Object));

        Assert.False(handler.TryCaptureOnChange<TimeoutPolicyOptions>(name));
        Assert.True(handler.TryCaptureOnChange<TimeoutPolicyOptions>(name));
    }

    private static IOnChangeListenersHandler GetHandler(Action<IServiceCollection> configure = null!)
    {
        var services = new ServiceCollection()
            .AddSingleton<IOnChangeListenersHandler, OnChangeListenersHandler>();
        services.AddOptions<RetryPolicyOptions>();
        services.AddOptions<TimeoutPolicyOptions>();
        configure?.Invoke(services);

        return services.BuildServiceProvider().GetRequiredService<IOnChangeListenersHandler>();
    }
}
