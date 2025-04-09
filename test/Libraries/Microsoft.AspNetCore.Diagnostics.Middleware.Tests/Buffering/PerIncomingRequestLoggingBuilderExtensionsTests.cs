// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#if NET9_0_OR_GREATER
using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Diagnostics.Buffering;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Buffering;
using Microsoft.Extensions.Options;
using Xunit;
using PerRequestLogBuffer = Microsoft.Extensions.Diagnostics.Buffering.PerRequestLogBuffer;

namespace Microsoft.Extensions.Logging;

public class PerIncomingRequestLoggingBuilderExtensionsTests
{
    [Fact]
    public void WhenLogLevelProvided_RegistersInDI()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddLogging(builder =>
        {
            builder.AddPerIncomingRequestBuffer(LogLevel.Warning);
        });

        ServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();
        var buffer = serviceProvider.GetService<PerRequestLogBuffer>();

        Assert.NotNull(buffer);
        Assert.IsAssignableFrom<PerRequestLogBufferManager>(buffer);
    }

    [Fact]
    public void WhenArgumentNull_Throws()
    {
        ILoggingBuilder? builder = null;
        IConfiguration? configuration = null;

        Assert.Throws<ArgumentNullException>(() => builder!.AddPerIncomingRequestBuffer(LogLevel.Warning));
        Assert.Throws<ArgumentNullException>(() => builder!.AddPerIncomingRequestBuffer(configuration!));
    }

    [Fact]
    public void WhenIConfigurationProvided_RegistersInDI()
    {
        List<LogBufferingFilterRule> expectedData =
        [
            new(categoryName: "Program.MyLogger", logLevel: LogLevel.Information, eventId: 1, eventName: "number one"),
            new(logLevel: LogLevel.Information),
        ];
        var configBuilder = new ConfigurationBuilder();
        configBuilder.AddJsonFile("appsettings.json");
        IConfigurationRoot configuration = configBuilder.Build();
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddLogging(b => b.AddPerIncomingRequestBuffer(configuration));
        ServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();
        var options = serviceProvider.GetService<IOptionsMonitor<PerRequestLogBufferingOptions>>();
        Assert.NotNull(options);
        Assert.NotNull(options.CurrentValue);
        Assert.Equivalent(expectedData, options.CurrentValue.Rules);
    }

    [Fact]
    public void WhenConfigurationActionProvided_RegistersInDI()
    {
        List<LogBufferingFilterRule> expectedData =
        [
            new(categoryName: "Program.MyLogger", logLevel: LogLevel.Information, eventId: 1, eventName: "number one"),
            new(logLevel: LogLevel.Information),
        ];
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddLogging(b => b.AddPerIncomingRequestBuffer(options =>
        {
            options.Rules.Add(new LogBufferingFilterRule(categoryName: "Program.MyLogger",
                logLevel: LogLevel.Information, eventId: 1, eventName: "number one"));
            options.Rules.Add(new LogBufferingFilterRule(logLevel: LogLevel.Information));
        }));
        ServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();
        var options = serviceProvider.GetService<IOptionsMonitor<PerRequestLogBufferingOptions>>();
        Assert.NotNull(options);
        Assert.NotNull(options.CurrentValue);
        Assert.Equivalent(expectedData, options.CurrentValue.Rules);
    }
}
#endif
