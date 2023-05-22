// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
#if NET5_0_OR_GREATER
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
#endif
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Testing;
using Microsoft.Extensions.Logging;
#if NET5_0_OR_GREATER
using Microsoft.Extensions.Options;
#endif
using Microsoft.Extensions.Telemetry.Logging;
#if NET5_0_OR_GREATER
using Moq;
#endif
using OpenTelemetry;
using OpenTelemetry.Logs;
using Xunit;

namespace Microsoft.Extensions.Telemetry.Console.Test;

public sealed class LoggingConsoleExtensionsTests
{
    [Fact]
    public void AddConsoleExporter_GivenInvalidArguments_ThrowsException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            ((ILoggingBuilder)null!).AddConsoleExporter());
#if NET5_0_OR_GREATER
        Assert.Throws<ArgumentNullException>(() =>
            ((ILoggingBuilder)null!).AddConsoleExporter((Action<LoggingConsoleOptions>)null!));
        Assert.Throws<ArgumentNullException>(() =>
            Mock.Of<ILoggingBuilder>().AddConsoleExporter((Action<LoggingConsoleOptions>)null!));
        Assert.Throws<ArgumentNullException>(() =>
            ((ILoggingBuilder)null!).AddConsoleExporter((IConfigurationSection)null!));
        Assert.Throws<ArgumentNullException>(() =>
            Mock.Of<ILoggingBuilder>().AddConsoleExporter((IConfigurationSection)null!));
#endif
    }

    [Fact]
    public void AddConsoleExporter_GivenNoArguments_RegistersRequiredServices()
    {
        using var host = FakeHost.CreateBuilder(options => options.FakeLogging = false)
            .ConfigureLogging(builder => builder
                .AddOpenTelemetryLogging()
                .AddConsoleExporter())
            .Build();

        var exporter = host.Services.GetService<BaseExporter<LogRecord>>();
        Assert.NotNull(exporter);
        Assert.IsAssignableFrom<LoggingConsoleExporter>(exporter);

        var processor = host.Services.GetService<BaseProcessor<LogRecord>>();
        Assert.NotNull(processor);
        Assert.IsAssignableFrom<SimpleLogRecordExportProcessor>(processor);
    }

#if NET5_0_OR_GREATER
    [Fact]
    public void AddConsoleExporter_GivenConfigAction_RegistersRequiredServices()
    {
        using var host = FakeHost.CreateBuilder(options => options.FakeLogging = false)
            .ConfigureLogging(builder => builder
                .AddOpenTelemetryLogging()
                .AddConsoleExporter(
                    exporterOptions =>
                    {
                        exporterOptions.IncludeLogLevel = false;
                        exporterOptions.IncludeCategory = false;
                        exporterOptions.ColorsEnabled = false;
                        exporterOptions.DimmedColor = ConsoleColor.Green;
                        exporterOptions.ExceptionStackTraceBackgroundColor = ConsoleColor.Yellow;
                    }))
            .Build();

        var exporter = host.Services.GetService<BaseExporter<LogRecord>>();
        Assert.NotNull(exporter);
        Assert.IsAssignableFrom<LoggingConsoleExporter>(exporter);

        var processor = host.Services.GetService<BaseProcessor<LogRecord>>();
        Assert.NotNull(processor);
        Assert.IsAssignableFrom<SimpleLogRecordExportProcessor>(processor);

        var options = host.Services.GetService<IOptions<LoggingConsoleOptions>>();
        Assert.NotNull(options);
        Assert.NotNull(options!.Value);
        Assert.False(options!.Value.IncludeLogLevel);
        Assert.False(options!.Value.IncludeCategory);
        Assert.False(options!.Value.ColorsEnabled);
        Assert.Equal(ConsoleColor.Green, options!.Value.DimmedColor);
        Assert.Equal(ConsoleColor.Yellow, options!.Value.ExceptionStackTraceBackgroundColor);
    }

    [Fact]
    public void AddConsoleExporter_GivenConfigSecion_RegistersRequiredServices()
    {
        using var host = FakeHost.CreateBuilder(options => options.FakeLogging = false)
            .ConfigureLogging(builder => builder
                .AddOpenTelemetryLogging()
                .AddConsoleExporter(GetConsoleLogFormatterConfigSection()))
            .Build();

        var exporter = host.Services.GetService<BaseExporter<LogRecord>>();
        Assert.NotNull(exporter);
        Assert.IsAssignableFrom<LoggingConsoleExporter>(exporter);

        var processor = host.Services.GetService<BaseProcessor<LogRecord>>();
        Assert.NotNull(processor);
        Assert.IsAssignableFrom<SimpleLogRecordExportProcessor>(processor);
        var options = host.Services.GetService<IOptions<LoggingConsoleOptions>>();
        Assert.NotNull(options);
        Assert.NotNull(options!.Value);
        Assert.False(options!.Value.ColorsEnabled);
        Assert.Equal(ConsoleColor.Cyan, options!.Value.DimmedColor);
    }

    private static IConfigurationSection GetConsoleLogFormatterConfigSection()
    {
        LoggingConsoleOptions options;

        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                    {
                        $"{nameof(LoggingConsoleOptions)}:{nameof(options.ColorsEnabled)}",
                        "false"
                    },
                    {
                        $"{nameof(LoggingConsoleOptions)}:{nameof(options.DimmedColor)}",
                        ConsoleColor.Cyan.ToString()
                    },
            })
            .Build()
            .GetSection($"{nameof(LoggingConsoleOptions)}");
    }
#endif
}
