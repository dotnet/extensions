// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Buffering;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.Extensions.Logging.Test;

public class GlobalBufferLoggerBuilderExtensionsTests
{
    [Fact]
    public void AddGlobalBuffer_RegistersInDI()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddLogging(builder =>
        {
            builder.AddGlobalBuffer(LogLevel.Warning);
        });

        var serviceProvider = serviceCollection.BuildServiceProvider();
        var bufferManager = serviceProvider.GetService<IBufferManager>();

        Assert.NotNull(bufferManager);
        Assert.IsAssignableFrom<GlobalBufferManager>(bufferManager);
    }

    [Fact]
    public void WhenArgumentNull_Throws()
    {
        var builder = null as ILoggingBuilder;
        var configuration = null as IConfiguration;

        Assert.Throws<ArgumentNullException>(() => builder!.AddGlobalBuffer(LogLevel.Warning));
        Assert.Throws<ArgumentNullException>(() => builder!.AddGlobalBuffer(configuration!));
    }

    [Fact]
    public void AddGlobalBufferConfiguration_RegistersInDI()
    {
        List<BufferFilterRule> expectedData =
        [
            new BufferFilterRule("Program.MyLogger", LogLevel.Information, 1, null),
            new BufferFilterRule(null, LogLevel.Information, null, null),
        ];
        ConfigurationBuilder configBuilder = new ConfigurationBuilder();
        configBuilder.AddJsonFile("appsettings.json");
        IConfigurationRoot configuration = configBuilder.Build();
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddLogging(builder =>
        {
            builder.AddGlobalBufferConfiguration(configuration);
        });
        var serviceProvider = serviceCollection.BuildServiceProvider();
        var options = serviceProvider.GetService<IOptionsMonitor<GlobalBufferOptions>>();
        Assert.NotNull(options);
        Assert.NotNull(options.CurrentValue);
        Assert.Equivalent(expectedData, options.CurrentValue.Rules);
    }
}
