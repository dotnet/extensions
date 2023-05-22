// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting.Testing.Internal;
using Microsoft.Extensions.Hosting.Testing.Test.TestResources;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Telemetry.Testing.Logging;
using Moq;
using Xunit;

namespace Microsoft.Extensions.Hosting.Testing.Test;

public class FakeHostBuilderTest
{
    private static readonly FakeHostOptions _noFakesOptions = new()
    {
        FakeLogging = false,
        FakeRedaction = false,
        ValidateScopes = false,
        ValidateOnBuild = false,
    };

    [Fact]
    public void Constructor_AddsFakeHostOptions()
    {
        var hostBuilderServices = new FakeHostBuilder(new FakeHostOptions { }).Build().Services;

        var options = hostBuilderServices.GetRequiredService<FakeHostOptions>();
        Assert.NotNull(options);
    }

    [Fact]
    public void Constructor_AddsHostTerminatorService()
    {
        var hostBuilderServices = new FakeHostBuilder(new FakeHostOptions()).Build().Services;
        Assert.Contains(hostBuilderServices.GetServices<IHostedService>(), x => x is HostTerminatorService);
    }

    [Fact]
    public void Constructor_FakesLogging()
    {
        var hostBuilderServices = new FakeHostBuilder(new FakeHostOptions()).Build().Services;

        Assert.NotNull(hostBuilderServices.GetService<FakeLogCollector>());
        Assert.IsType<FakeLogger>(hostBuilderServices.GetService<ILogger>());
    }

    [Fact]
    public void Constructor_FakeLoggingFalse_DoesNotFakeLogging()
    {
        var hostBuilderServices = new FakeHostBuilder(new FakeHostOptions { FakeLogging = false }).Build().Services;

        Assert.Null(hostBuilderServices.GetService<FakeLogCollector>());
    }

    [Fact]
    public void ConfigureHostConfiguration_CallsWrappedInstance()
    {
        var configurationDelegate = (IConfigurationBuilder _) => { };
        var builderMock = new Mock<IHostBuilder>();
        builderMock.Setup(x => x.ConfigureHostConfiguration(configurationDelegate)).Returns(builderMock.Object);

        var builder = new FakeHostBuilder(builderMock.Object, _noFakesOptions);
        var returnedBuilder = builder.ConfigureHostConfiguration(configurationDelegate);

        Assert.Equal(builderMock.Object, returnedBuilder);
    }

    [Fact]
    public void ConfigureAppConfiguration_CallsWrappedInstance()
    {
        var configurationDelegate = (HostBuilderContext _, IConfigurationBuilder _) => { };
        var builderMock = new Mock<IHostBuilder>();
        builderMock.Setup(x => x.ConfigureAppConfiguration(configurationDelegate)).Returns(builderMock.Object);

        var builder = new FakeHostBuilder(builderMock.Object, _noFakesOptions);
        var returnedBuilder = builder.ConfigureAppConfiguration(configurationDelegate);

        Assert.Equal(builderMock.Object, returnedBuilder);
    }

    [Fact]
    public void Properties_UsesWrappedInstance()
    {
        IDictionary<object, object> properties = new Dictionary<object, object>();
        var builderMock = new Mock<IHostBuilder>();
        builderMock.SetupGet(x => x.Properties)
            .Returns(properties);

        var builder = new FakeHostBuilder(builderMock.Object, _noFakesOptions);

        Assert.Same(properties, builder.Properties);
    }

    [Fact]
    public void ConfigureContainer_CallsWrappedInstance()
    {
        var configurationDelegate = (HostBuilderContext _, object _) => { };
        var builderMock = new Mock<IHostBuilder>();
        builderMock.Setup(x => x.ConfigureContainer(configurationDelegate)).Returns(builderMock.Object);

        var builder = new FakeHostBuilder(builderMock.Object, _noFakesOptions);
        var returnedBuilder = builder.ConfigureContainer(configurationDelegate);

        Assert.Equal(builderMock.Object, returnedBuilder);
    }

    [Fact]
    public void UseServiceProviderFactory_CallsWrappedInstance()
    {
        var factory = new Mock<IServiceProviderFactory<object>>().Object;
        var builderMock = new Mock<IHostBuilder>();
        builderMock.Setup(x => x.UseServiceProviderFactory(factory))
            .Returns(builderMock.Object);

        var builder = new FakeHostBuilder(builderMock.Object, _noFakesOptions);
        var returnedBuilder = builder.UseServiceProviderFactory(factory);

        Assert.Equal(builderMock.Object, returnedBuilder);
    }

    [Fact]
    public void Build_ValidatesScopes()
    {
        var hostBuilder = FakeHost.CreateBuilder()
            .ConfigureServices((_, services) =>
            {
                services.AddScoped<InnerClass>()
                    .AddSingleton<OuterClass>();
            });

        var exception = Record.Exception(() => hostBuilder.Build());

        Assert.IsType<AggregateException>(exception);
        Assert.Collection(
            ((AggregateException)exception).InnerExceptions,
            x => Assert.IsType<InvalidOperationException>(x));
    }

    [Fact]
    public void Build_ValidateScopesFalse_DoesNotValidateScopes()
    {
        var hostBuilder = FakeHost.CreateBuilder(x => x.ValidateScopes = false)
            .ConfigureServices((_, services) =>
            {
                services.AddScoped<InnerClass>()
                    .AddSingleton<OuterClass>();
            });

        var exception = Record.Exception(() => hostBuilder.Build());

        Assert.Null(exception);
    }

    [Fact]
    public void Build_ValidatesDependenciesOnBuild()
    {
        var hostBuilder = FakeHost.CreateBuilder()
            .ConfigureServices((_, services) =>
            {
                services.AddSingleton<DependentClass>();
            });

        var exception = Record.Exception(() => hostBuilder.Build());

        Assert.IsType<AggregateException>(exception);
        Assert.Collection(
            ((AggregateException)exception).InnerExceptions,
            x => Assert.IsType<InvalidOperationException>(x));
    }

    [Fact]
    public void Build_ValidateOnBuildFalse_DoesNotValidateOnBuild()
    {
        var hostBuilder = FakeHost.CreateBuilder(x => x.ValidateOnBuild = false)
            .ConfigureServices((_, services) =>
            {
                services.AddSingleton<DependentClass>();
            });

        var exception = Record.Exception(() => hostBuilder.Build());

        Assert.Null(exception);
    }

    [Fact]
    public void UseNewServiceProviderFactory_CallsWrappedInstance()
    {
        var factory = new Mock<IServiceProviderFactory<object>>().Object;
        var functor = (HostBuilderContext _) => factory;
        var builderMock = new Mock<IHostBuilder>();
        builderMock.Setup(x => x.UseServiceProviderFactory(functor))
            .Returns(builderMock.Object);

        var builder = new FakeHostBuilder(builderMock.Object, _noFakesOptions);
        var returnedBuilder = builder.UseServiceProviderFactory(functor);

        Assert.Equal(builderMock.Object, returnedBuilder);
    }
}
