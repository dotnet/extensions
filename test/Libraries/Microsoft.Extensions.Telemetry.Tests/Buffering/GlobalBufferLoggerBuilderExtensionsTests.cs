// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if NET9_0_OR_GREATER
using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
        var buffer = serviceProvider.GetService<ILoggingBuffer>();

        Assert.NotNull(buffer);
        Assert.IsAssignableFrom<GlobalBuffer>(buffer);
    }

    [Fact]
    public void WhenArgumentNull_Throws()
    {
        var builder = null as ILoggingBuilder;
        var configuration = null as IConfiguration;

        Assert.Throws<ArgumentNullException>(() => builder!.AddGlobalBuffer(LogLevel.Warning));
        Assert.Throws<ArgumentNullException>(() => builder!.AddGlobalBuffer(configuration!));
        Assert.Throws<ArgumentNullException>(() => builder!.AddGlobalBufferProvider());
    }

    [Fact]
    public void AddGlobalBufferConfiguration_RegistersInDI()
    {
        List<BufferFilterRule> expectedData =
        [
            new BufferFilterRule("Program.MyLogger", LogLevel.Information, 1),
            new BufferFilterRule(null, LogLevel.Information, null),
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
#endif
