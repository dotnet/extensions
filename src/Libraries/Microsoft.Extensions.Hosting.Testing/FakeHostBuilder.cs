// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Compliance.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;

namespace Microsoft.Extensions.Hosting.Testing;

internal sealed class FakeHostBuilder : IHostBuilder
{
    private readonly IHostBuilder _builder;
    private readonly FakeHostOptions _options;

    internal FakeHostBuilder(FakeHostOptions options)
        : this(new HostBuilder(), options)
    {
    }

    internal FakeHostBuilder(IHostBuilder builder, FakeHostOptions options)
    {
        _options = options;
        _builder = builder;

        _ = builder
            .ConfigureServices(services => services
                .AddSingleton(options)
                .AddHostedService<HostTerminatorService>());

        if (options.FakeLogging)
        {
            _ = _builder.ConfigureServices(services =>
            {
                services
                    .AddFakeLogging()
                    .TryAddSingleton<ILogger, FakeLogger>();

            });
        }

        if (options.FakeRedaction)
        {
            _builder = _builder.ConfigureServices(services => services.AddFakeRedaction());
        }

        if (options.ValidateScopes || options.ValidateOnBuild)
        {
            var serviceProviderOptions = new ServiceProviderOptions
            {
                ValidateScopes = options.ValidateScopes,
                ValidateOnBuild = options.ValidateOnBuild
            };

            _builder = _builder.UseServiceProviderFactory(new DefaultServiceProviderFactory(serviceProviderOptions));
        }
    }

    public IHostBuilder ConfigureHostConfiguration(Action<IConfigurationBuilder> configureDelegate)
    {
        return _builder.ConfigureHostConfiguration(configureDelegate);
    }

    public IHostBuilder ConfigureAppConfiguration(Action<HostBuilderContext, IConfigurationBuilder> configureDelegate)
    {
        return _builder.ConfigureAppConfiguration(configureDelegate);
    }

    public IHostBuilder ConfigureServices(Action<HostBuilderContext, IServiceCollection> configureDelegate)
    {
        return _builder.ConfigureServices(configureDelegate);
    }

    public IHostBuilder UseServiceProviderFactory<TContainerBuilder>(IServiceProviderFactory<TContainerBuilder> factory)
        where TContainerBuilder : notnull
    {
        return _builder.UseServiceProviderFactory(factory);
    }

    public IHostBuilder UseServiceProviderFactory<TContainerBuilder>(Func<HostBuilderContext, IServiceProviderFactory<TContainerBuilder>> factory)
        where TContainerBuilder : notnull
    {
        return _builder.UseServiceProviderFactory(factory);
    }

    public IHostBuilder ConfigureContainer<TContainerBuilder>(Action<HostBuilderContext, TContainerBuilder> configureDelegate) => _builder.ConfigureContainer(configureDelegate);

    public IHost Build() => new FakeHost(_builder.Build(), _options);

    public IDictionary<object, object> Properties => _builder.Properties;
}
