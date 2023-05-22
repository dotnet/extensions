// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting.Testing.Internal;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Microsoft.Extensions.Hosting.Testing.Test;

public class HostingFakesExtensionsTest
{
    [Fact]
    public async Task StartAndStop_NullGiven_Throws()
    {
        var exception = await Record.ExceptionAsync(() => ((IHostedService)null!).StartAndStopAsync());
        Assert.IsType<ArgumentNullException>(exception);
    }

    [Fact]
    public async Task StartAndStop_ServiceGiven_StartsAndStopsTheService()
    {
        using var tokenSource = new CancellationTokenSource();

        var serviceMock = new Mock<IHostedService>(MockBehavior.Strict);
        serviceMock.Setup(x => x.StartAsync(tokenSource.Token)).Returns(Task.CompletedTask);
        serviceMock.Setup(x => x.StopAsync(tokenSource.Token)).Returns(Task.CompletedTask);

        await serviceMock.Object.StartAndStopAsync(tokenSource.Token);

        serviceMock.VerifyAll();
    }

    [Fact]
    public async Task GetFakeLogCollector_FetchesGetFakeLogCollector()
    {
        using var host = await FakeHost.CreateBuilder().StartAsync();
        Assert.NotNull(host.GetFakeLogCollector());
    }

    [Fact]
    public void GetFakeLogCollector_FakeCollectorMissing_ThrowsException()
    {
        using var host = new HostBuilder().Build();

        var exception = Record.Exception(() => host.GetFakeLogCollector());

        Assert.NotNull(exception);
        Assert.IsType<InvalidOperationException>(exception);
        Assert.Equal("No fake log collector registered", exception.Message);
    }

    [Fact]
    public async Task GetFakeRedactionCollector_FetchesFakeRedactionCollector()
    {
        using var host = await FakeHost.CreateBuilder().StartAsync();

        var collector = host.GetFakeRedactionCollector();

        Assert.NotNull(collector);
    }

    [Fact]
    public void GetFakeRedactionCollector_FakeCollectorMissing_ThrowsException()
    {
        using var host = new HostBuilder().Build();

        var exception = Record.Exception(() => host.GetFakeRedactionCollector());

        Assert.NotNull(exception);
        Assert.IsType<InvalidOperationException>(exception);
    }

    [Fact]
    public void Configure_delegateUsed_ConfiguresGivenBuilder()
    {
        var builderMock = new Mock<IHostBuilder>();

        var returnedBuilder = builderMock.Object.Configure(builder => builder.Build());

        Assert.Equal(builderMock.Object, returnedBuilder);
        builderMock.Verify(mock => mock.Build(), Times.Once);
    }

    [Fact]
    public void Configure_BuilderIsNull_Throws()
    {
        var exception = Record.Exception(() => ((IHostBuilder)null!).Configure(_ => { }));
        Assert.IsType<ArgumentNullException>(exception);
    }

    [Fact]
    public void Configure_ConfigureDelegateIsNull_Throws()
    {
        var exception = Record.Exception(() => new HostBuilder().Configure(null!));
        Assert.IsType<ArgumentNullException>(exception);
    }

    [Fact]
    public void Configure_nullDelegate_Throws()
    {
        var builderMock = new Mock<IHostBuilder>();

        var exception = Record.Exception(() => builderMock.Object.Configure(null!));
        Assert.IsType<ArgumentNullException>(exception);
    }

    [Fact]
    public void ConfigureAppConfiguration_ReturnsBuilder()
    {
        var builderMock = new Mock<IHostBuilder>();
        builderMock
            .Setup(x => x.ConfigureAppConfiguration(It.IsAny<Action<HostBuilderContext, IConfigurationBuilder>>()))
            .Returns(builderMock.Object);

        var returnedBuilder = builderMock.Object.ConfigureAppConfiguration("testKey", "testValue");

        Assert.Equal(builderMock.Object, returnedBuilder);
    }

    [Fact]
    public void ConfigureAppConfiguration_KeyAndValueGiven_AddsToConfiguration()
    {
        var configBuilder = new ConfigurationBuilder();
        configBuilder.Sources.Add(new ChainedConfigurationSource());

        var builderMock = CreateHostBuilderMock(appConfigBuilder: configBuilder);

        _ = builderMock.Object.ConfigureAppConfiguration("testKey", "testValue");

        Assert.Collection(
            configBuilder.Sources,
            source => Assert.IsType<ChainedConfigurationSource>(source),
            source =>
            {
                Assert.IsType<FakeConfigurationSource>(source);
                Assert.Collection(
                    ((FakeConfigurationSource)source).InitialData!,
                    item =>
                    {
                        Assert.Equal("testKey", item.Key);
                        Assert.Equal("testValue", item.Value);
                    });
            });
    }

    [Fact]
    public void ConfigureAppConfiguration_MultipleKeyAndValueGiven_AddsToConfiguration()
    {
        var configBuilder = new ConfigurationBuilder();
        configBuilder.Sources.Add(new ChainedConfigurationSource());

        var builderMock = CreateHostBuilderMock(appConfigBuilder: configBuilder);

        _ = builderMock.Object.ConfigureAppConfiguration(("testKey1", "testValue1"), ("testKey2", "testValue2"));

        Assert.Collection(
            configBuilder.Sources,
            source => Assert.IsType<ChainedConfigurationSource>(source),
            source =>
            {
                Assert.IsType<FakeConfigurationSource>(source);
                Assert.Collection(
                    ((FakeConfigurationSource)source).InitialData!,
                    item =>
                    {
                        Assert.Equal("testKey1", item.Key);
                        Assert.Equal("testValue1", item.Value);
                    },
                    item =>
                    {
                        Assert.Equal("testKey2", item.Key);
                        Assert.Equal("testValue2", item.Value);
                    });
            });
    }

    [Fact]
    public void ConfigureAppConfiguration_MultipleKeyAndValueGiven_AddsOnlyOneConfigurationSource()
    {
        var configBuilder = new ConfigurationBuilder();
        var builderMock = CreateHostBuilderMock(appConfigBuilder: configBuilder);

        _ = builderMock.Object.ConfigureAppConfiguration("testKey", "testValue");
        _ = builderMock.Object.ConfigureAppConfiguration("anotherTestKey", "anotherTestValue");

        Assert.Collection(
            configBuilder.Sources,
            source =>
            {
                Assert.IsType<FakeConfigurationSource>(source);
                Assert.Collection(
                    ((FakeConfigurationSource)source).InitialData!,
                    item =>
                    {
                        Assert.Equal("testKey", item.Key);
                        Assert.Equal("testValue", item.Value);
                    },
                    item =>
                    {
                        Assert.Equal("anotherTestKey", item.Key);
                        Assert.Equal("anotherTestValue", item.Value);
                    });
            });
    }

    [Fact]
    public void ConfigureHostConfiguration_KeyAndValueGiven_AddsToConfiguration()
    {
        var configBuilder = new ConfigurationBuilder();
        configBuilder.Sources.Add(new ChainedConfigurationSource());

        var builderMock = CreateHostBuilderMock(hostConfigBuilder: configBuilder);

        _ = builderMock.Object.ConfigureHostConfiguration("testKey", "testValue");

        Assert.Collection(
            configBuilder.Sources,
            source => Assert.IsType<ChainedConfigurationSource>(source),
            source =>
            {
                Assert.IsType<FakeConfigurationSource>(source);
                Assert.Collection(
                    ((FakeConfigurationSource)source).InitialData!,
                    item =>
                    {
                        Assert.Equal("testKey", item.Key);
                        Assert.Equal("testValue", item.Value);
                    });
            });
    }

    [Fact]
    public void ConfigureHostConfiguration_MultipleEntriesGiven_AddsToConfiguration()
    {
        var configBuilder = new ConfigurationBuilder();
        configBuilder.Sources.Add(new ChainedConfigurationSource());

        var builderMock = CreateHostBuilderMock(hostConfigBuilder: configBuilder);

        _ = builderMock.Object.ConfigureHostConfiguration(("testKey1", "testValue1"), ("testKey2", "testValue2"));

        Assert.Collection(
            configBuilder.Sources,
            source => Assert.IsType<ChainedConfigurationSource>(source),
            source =>
            {
                Assert.IsType<FakeConfigurationSource>(source);
                Assert.Collection(
                    ((FakeConfigurationSource)source).InitialData!,
                    item =>
                    {
                        Assert.Equal("testKey1", item.Key);
                        Assert.Equal("testValue1", item.Value);
                    },
                    item =>
                    {
                        Assert.Equal("testKey2", item.Key);
                        Assert.Equal("testValue2", item.Value);
                    });
            });
    }

    [Fact]
    public void ConfigureHostConfiguration_MultipleKeyAndValueGiven_AddsOnlyOneConfigurationSource()
    {
        var configBuilder = new ConfigurationBuilder();
        var builderMock = CreateHostBuilderMock(hostConfigBuilder: configBuilder);

        _ = builderMock.Object.ConfigureHostConfiguration("testKey", "testValue");
        _ = builderMock.Object.ConfigureHostConfiguration("anotherTestKey", "anotherTestValue");

        Assert.Collection(
            configBuilder.Sources,
            source =>
            {
                Assert.IsType<FakeConfigurationSource>(source);
                Assert.Collection(
                    ((FakeConfigurationSource)source).InitialData!,
                    item =>
                    {
                        Assert.Equal("testKey", item.Key);
                        Assert.Equal("testValue", item.Value);
                    },
                    item =>
                    {
                        Assert.Equal("anotherTestKey", item.Key);
                        Assert.Equal("anotherTestValue", item.Value);
                    });
            });
    }

    [Fact]
    public void AddLoggingCallback_NullCallback_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => FakeHost.CreateBuilder().AddFakeLoggingOutputSink(null!));
    }

    [Fact]
    public void AddLoggingCallback_NullHostBuilder_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => ((IHostBuilder)null!).AddFakeLoggingOutputSink(_ => { }));
    }

    [Fact]
    public async Task AddLoggingCallback_CallbackUsed_AddsCallback()
    {
        var message = Guid.NewGuid().ToString();
        var firstCallbackTarget = new List<string>();
        var secondCallbackTarget = new List<string>();
        using var host = await FakeHost.CreateBuilder()
            .AddFakeLoggingOutputSink(firstCallbackTarget.Add)
            .AddFakeLoggingOutputSink(secondCallbackTarget.Add)
            .StartAsync();

        var logger = host.Services.GetRequiredService<ILogger>();
        logger.LogWarning(message);

        Assert.Contains(firstCallbackTarget, record => record.Contains(message));
        Assert.Contains(secondCallbackTarget, record => record.Contains(message));
    }

    private static Mock<IHostBuilder> CreateHostBuilderMock(
        IConfigurationBuilder? appConfigBuilder = null,
        IConfigurationBuilder? hostConfigBuilder = null)
    {
        var builderMock = new Mock<IHostBuilder>();

        if (appConfigBuilder is not null)
        {
            builderMock
                .Setup(x => x.ConfigureAppConfiguration(It.IsAny<Action<HostBuilderContext, IConfigurationBuilder>>()))
                .Returns(builderMock.Object)
                .Callback<Action<HostBuilderContext?, IConfigurationBuilder>>(configure => configure(null, appConfigBuilder));
        }

        if (hostConfigBuilder is not null)
        {
            builderMock
                .Setup(x => x.ConfigureHostConfiguration(It.IsAny<Action<IConfigurationBuilder>>()))
                .Returns(builderMock.Object)
                .Callback<Action<IConfigurationBuilder>>(configure => configure(hostConfigBuilder));
        }

        return builderMock;
    }
}
