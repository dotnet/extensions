// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Buffering;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.AspNetCore.Diagnostics.Buffering.Test;

public class HttpRequestBufferLoggerBuilderExtensionsTests
{
    [Fact]
    public void AddHttpRequestBuffering_RegistersInDI()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddLogging(builder =>
        {
            builder.AddHttpRequestBuffering(LogLevel.Warning);
        });

        var serviceProvider = serviceCollection.BuildServiceProvider();
        var buffer = serviceProvider.GetService<HttpRequestLogBuffer>();

        Assert.NotNull(buffer);
        Assert.IsAssignableFrom<HttpRequestBufferManager>(buffer);
    }

    [Fact]
    public void WhenArgumentNull_Throws()
    {
        var builder = null as ILoggingBuilder;
        var configuration = null as IConfiguration;

        Assert.Throws<ArgumentNullException>(() => builder!.AddHttpRequestBuffering(LogLevel.Warning));
        Assert.Throws<ArgumentNullException>(() => builder!.AddHttpRequestBuffering(configuration!));
    }

    [Fact]
    public void AddHttpRequestBufferConfiguration_RegistersInDI()
    {
        List<LogBufferingFilterRule> expectedData =
        [
            new LogBufferingFilterRule(categoryName: "Program.MyLogger", logLevel: LogLevel.Information, eventId: 1, eventName: "number one"),
            new LogBufferingFilterRule(logLevel: LogLevel.Information),
        ];
        ConfigurationBuilder configBuilder = new ConfigurationBuilder();
        configBuilder.AddJsonFile("appsettings.json");
        IConfigurationRoot configuration = configBuilder.Build();
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddLogging(b => b.AddHttpRequestBuffering(configuration));
        var serviceProvider = serviceCollection.BuildServiceProvider();
        var options = serviceProvider.GetService<IOptionsMonitor<HttpRequestLogBufferingOptions>>();
        Assert.NotNull(options);
        Assert.NotNull(options.CurrentValue);
        Assert.Equivalent(expectedData, options.CurrentValue.Rules);
    }
}
