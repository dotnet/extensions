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

namespace Microsoft.Extensions.Diagnostics.Buffering.Test;

public class GlobalBufferLoggerBuilderExtensionsTests
{
    [Fact]
    public void WithLogLevel_RegistersInDI()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddLogging(builder =>
        {
            builder.AddGlobalBuffer(LogLevel.Warning);
        });

        ServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();
        var bufferManager = serviceProvider.GetService<GlobalLogBuffer>();

        Assert.NotNull(bufferManager);
        Assert.IsAssignableFrom<GlobalLogBufferManager>(bufferManager);
    }

    [Fact]
    public void WhenArgumentNull_Throws()
    {
        ILoggingBuilder? builder = null;
        IConfiguration? configuration = null;

        Assert.Throws<ArgumentNullException>(() => builder!.AddGlobalBuffer(LogLevel.Warning));
        Assert.Throws<ArgumentNullException>(() => builder!.AddGlobalBuffer(configuration!));
    }

    [Fact]
    public void WithConfiguration_RegistersInDI()
    {
        List<LogBufferingFilterRule> expectedData =
        [
            new ("Program.MyLogger",  LogLevel.Information, 1, "number one", [new("region", "westus2"), new ("priority", 1)]),
            new (logLevel: LogLevel.Information),
        ];
        ConfigurationBuilder configBuilder = new ConfigurationBuilder();
        configBuilder.AddJsonFile("appsettings.json");
        IConfigurationRoot configuration = configBuilder.Build();
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddLogging(builder =>
        {
            builder.AddGlobalBuffer(configuration);
            builder.Services.Configure<GlobalLogBufferingOptions>(options =>
            {
                options.MaxLogRecordSizeInBytes = 33;
            });
        });
        ServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();
        var options = serviceProvider.GetService<IOptionsMonitor<GlobalLogBufferingOptions>>();
        Assert.NotNull(options);
        Assert.NotNull(options.CurrentValue);
        Assert.Equal(33, options.CurrentValue.MaxLogRecordSizeInBytes); // value comes from the Configure<GlobalLogBufferingOptions>()  call
        Assert.Equal(1000, options.CurrentValue.MaxBufferSizeInBytes); // value comes from appsettings.json
        Assert.Equal(TimeSpan.FromSeconds(30), options.CurrentValue.AutoFlushDuration); // value comes from default
        Assert.Equivalent(expectedData, options.CurrentValue.Rules);
    }
}
#endif
