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

namespace Microsoft.AspNetCore.Diagnostics.Logging.Test;

public class HttpRequestBufferLoggerBuilderExtensionsTests
{
    [Fact]
    public void AddHttpRequestBuffer_RegistersInDI()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddLogging(builder =>
        {
            builder.AddHttpRequestBuffer(LogLevel.Warning);
        });

        var serviceProvider = serviceCollection.BuildServiceProvider();
        var buffer = serviceProvider.GetService<ILoggingBuffer>();

        Assert.NotNull(buffer);
        Assert.IsAssignableFrom<HttpRequestBuffer>(buffer);
    }

    [Fact]
    public void AddHttpRequestBufferProvider_RegistersInDI()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddLogging(builder =>
        {
            builder.AddHttpRequestBufferProvider();
        });
        var serviceProvider = serviceCollection.BuildServiceProvider();
        var bufferProvider = serviceProvider.GetService<ILoggingBufferProvider>();
        Assert.NotNull(bufferProvider);
        Assert.IsAssignableFrom<HttpRequestBufferProvider>(bufferProvider);
    }

    [Fact]
    public void WhenArgumentNull_Throws()
    {
        var builder = null as ILoggingBuilder;
        var configuration = null as IConfiguration;

        Assert.Throws<ArgumentNullException>(() => builder!.AddHttpRequestBuffer(LogLevel.Warning));
        Assert.Throws<ArgumentNullException>(() => builder!.AddHttpRequestBuffer(configuration!));
        Assert.Throws<ArgumentNullException>(() => builder!.AddHttpRequestBufferProvider());
    }

    [Fact]
    public void AddHttpRequestBufferConfiguration_RegistersInDI()
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
            builder.AddHttpRequestBufferConfiguration(configuration);
        });
        var serviceProvider = serviceCollection.BuildServiceProvider();
        var options = serviceProvider.GetService<IOptionsMonitor<HttpRequestBufferOptions>>();
        Assert.NotNull(options);
        Assert.NotNull(options.CurrentValue);
        Assert.Equivalent(expectedData, options.CurrentValue.Rules);
    }
}
#endif
