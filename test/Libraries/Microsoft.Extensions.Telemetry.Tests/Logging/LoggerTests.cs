// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Testing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Telemetry.Enrichment;
using Microsoft.Extensions.Telemetry.Logging.Test.Internals;
using OpenTelemetry;
using OpenTelemetry.Logs;
using Xunit;
using IOptions = Microsoft.Extensions.Options.Options;

namespace Microsoft.Extensions.Telemetry.Logging.Test;

/// <summary>
/// Test class for R9 Logger.
/// </summary>
public sealed class LoggerTests
{
    private static LoggerProvider CreateLoggerProvider(
        IOptions<LoggingOptions>? loggingOptions = null,
        IEnumerable<ILogEnricher>? enrichers = null,
        IEnumerable<BaseProcessor<LogRecord>>? processors = null) => new(
            loggingOptions ?? IOptions.Create(new LoggingOptions()),
            enrichers ?? Enumerable.Empty<ILogEnricher>(),
            processors ?? Enumerable.Empty<BaseProcessor<LogRecord>>());

    [Fact]
    public void CreateLoggerWithNullConfigurationActionThrows()
    {
        Action<LoggingOptions>? nullAction = null;
        Assert.Throws<ArgumentNullException>(() => LoggerFactory.Create(builder => builder.AddOpenTelemetryLogging(nullAction!)));
    }

    [Fact]
    public void CreateLoggerWithNullConfigurationSectionThrows()
    {
        IConfigurationSection? nullSection = null;
        Assert.Throws<ArgumentNullException>(() => LoggerFactory.Create(builder => builder.AddOpenTelemetryLogging(nullSection!)));
    }

    [Fact]
    public void CreateLoggerWithNullProcessor()
    {
        Assert.Throws<ArgumentNullException>(() => LoggerFactory.Create(builder =>
            builder.AddOpenTelemetryLogging().AddProcessor(null!)));
    }

    [Fact]
    public void CreateLoggerWithSingleProcessor()
    {
        TestExporter exporter = new TestExporter();
        using var loggerFactory = LoggerFactory.Create(builder =>
            builder.AddOpenTelemetryLogging().AddProcessor(new SimpleLogRecordExportProcessor(exporter)));

        const string LogMessage = "This is testing {user}";
        ILogger logger = loggerFactory.CreateLogger("R9");
        logger.LogError(LogMessage, "testUser");

        var dictExpected = new Dictionary<string, object>
        {
            { "{OriginalFormat}", LogMessage },
            { "user", "testUser" }
        };

        Assert.True(Helpers.CompareStateValues(exporter.FirstState!, dictExpected));
        exporter.Dispose();

        // disposing the object again should not cause any issues
        exporter.Dispose();
    }

    [Fact]
    public void CreateLoggerEnablesOpenTelemetrySDK()
    {
        using var loggerFactory = LoggerFactory.Create(builder =>
            builder.AddOpenTelemetryLogging());

        // testing side effects because it is not possible to test OpenTelemetry .NET SDK class normally.
        Assert.True(Activity.ForceDefaultIdFormat);
    }

    [Fact]
    public void CreateLoggerWithErrorLevelLogging()
    {
        using var exporter = new TestExporter();
        using var loggerFactory = LoggerFactory.Create(builder => builder
            .SetMinimumLevel(LogLevel.Error)
            .AddOpenTelemetryLogging()
            .AddProcessor(new SimpleLogRecordExportProcessor(exporter)));

        var logMessage = "This is testing {user}";
        ILogger logger = loggerFactory.CreateLogger("R9");

        logger.LogInformation(logMessage, "testUser");
        Assert.Null(exporter.FirstLogRecord);

        logger.LogError(logMessage, "testUser");
        var dictExpected = new Dictionary<string, object>
        {
            { "{OriginalFormat}", logMessage },
            { "user", "testUser" }
        };

        Assert.True(Helpers.CompareStateValues(exporter.FirstState!, dictExpected));
    }

    [Fact]
    public void CreateLoggerWithLoggingLevelNone()
    {
        using var exporter = new TestExporter();
        using var loggerFactory = LoggerFactory.Create(builder => builder
            .SetMinimumLevel(LogLevel.None)
            .AddOpenTelemetryLogging()
            .AddProcessor(new SimpleLogRecordExportProcessor(exporter)));

        var logMessage = "This is testing {user}";
        ILogger logger = loggerFactory.CreateLogger("R9");

        logger.LogDebug(logMessage, "testUser");
        Assert.Null(exporter.FirstLogRecord);

        logger.LogInformation(logMessage, "testUser");
        Assert.Null(exporter.FirstLogRecord);

        logger.LogWarning(logMessage, "testUser");
        Assert.Null(exporter.FirstLogRecord);

        logger.LogError(logMessage, "testUser");
        Assert.Null(exporter.FirstLogRecord);
    }

    [Fact]
    public void CreateLoggerWithMultipleProcessor()
    {
        using var exporter = new TestExporter();
        using var loggerFactory = LoggerFactory.Create(builder => builder
            .AddOpenTelemetryLogging()
            .AddProcessor(new TestProcessor())
            .AddProcessor(new SimpleLogRecordExportProcessor(exporter)));

        var logMessage = "This is testing {user}";
        ILogger logger = loggerFactory.CreateLogger("R9");
        logger.LogError(logMessage, "testUser");

        var dictExpected = new Dictionary<string, object>
        {
            { "{OriginalFormat}", logMessage },
            { "user", "testUser" }
        };

        Assert.True(Helpers.CompareStateValues(exporter.FirstState!, dictExpected));
    }

    [Fact]
    public void CreateLoggerWithScopes()
    {
        using var exporter = new TestExporter();
        using var loggerFactory = LoggerFactory.Create(builder => builder
            .AddOpenTelemetryLogging(options => options.IncludeScopes = true)
            .AddProcessor(new SimpleLogRecordExportProcessor(exporter)));

        var logMessage = "This is testing {user}";
        var scopeName = "Adding Outer Scope";
        ILogger logger = loggerFactory.CreateLogger("R9");
        var dictExpected = new Dictionary<string, object>
        {
            { "{OriginalFormat}", logMessage },
            { "user", "testUser" },
        };

        using (logger.BeginScope("{ScopeMessage}", scopeName))
        {
            logger.LogError(logMessage, "testUser");
        }

        Assert.True(Helpers.CompareStateValues(exporter.FirstState!, dictExpected));
        Assert.Equal(scopeName, exporter.FirstScope!.Value.Value);
    }

    [Fact]
    public void LogDifferentTState()
    {
        using var exporter = new TestExporter();
        using var loggerFactory = LoggerFactory.Create(builder => builder
            .AddOpenTelemetryLogging()
            .AddProcessor(new SimpleLogRecordExportProcessor(exporter)));

        ILogger logger = loggerFactory.CreateLogger("R9");

        logger.Log<object>(LogLevel.Error, default, null!, null, (_, _) => string.Empty);
        Assert.Empty(exporter.FirstState!);

        logger.Log<object>(LogLevel.Error, default, "Hello", null, (_, _) => string.Empty);
        Assert.Equal("Hello", exporter.FirstState![0].Value);
        Assert.Equal("{OriginalFormat}", exporter.FirstState![0].Key);

        logger.Log<object>(LogLevel.Error, default, new[] { new KeyValuePair<string, object?>("Hello", "World") }, null, (_, _) => string.Empty);
        Assert.Equal("Hello", exporter.FirstState[0].Key);
        Assert.Equal("World", exporter.FirstState[0].Value);

        logger.Log<object>(LogLevel.Error, default, new[] { new KeyValuePair<string, object?>("Hello", "World") }.Take(1), null, (_, _) => string.Empty);
        Assert.Equal("Hello", exporter.FirstState[0].Key);
        Assert.Equal("World", exporter.FirstState[0].Value);

        logger.Log<object>(LogLevel.Error, default, 50, null, (_, _) => string.Empty);
        Assert.Equal(50, exporter.FirstState![0].Value);
        Assert.Equal("{OriginalFormat}", exporter.FirstState![0].Key);

        var helper = LogMethodHelper.GetHelper();
        helper.Add("Hello", "World");
        logger.Log<object>(LogLevel.Error, default, helper, null, (_, _) => string.Empty);
        Assert.Equal("Hello", exporter.FirstState[0].Key);
        Assert.Equal("World", exporter.FirstState[0].Value);
    }

    [Fact]
    public void LogException_IncludeStackTrace_Disabled()
    {
        using var exporter = new TestExporter();
        using var loggerFactory = LoggerFactory.Create(builder => builder
            .AddOpenTelemetryLogging()
            .AddProcessor(new SimpleLogRecordExportProcessor(exporter)));

        ILogger logger = loggerFactory.CreateLogger("R9");
        logger.LogError(new NotImplementedException(), "Method is not implemented");

        Assert.False(exporter.FirstState!.TryGetStackTrace(out string? stackTrace));
    }

    [Fact]
    public void LogException_WithoutInnerStack_IncludeStackTrace_Enabled()
    {
        using var exporter = new TestExporter();
        using var loggerFactory = LoggerFactory.Create(builder => builder
            .AddOpenTelemetryLogging(options => options.IncludeStackTrace = true)
            .AddProcessor(new SimpleLogRecordExportProcessor(exporter)));

        ILogger logger = loggerFactory.CreateLogger("R9");

        LoggerTests.ThrowExceptionAndLogError(() => TestExceptionThrower.ThrowExceptionWithoutInnerException(), logger);

        bool stackTraceReturned = exporter.FirstState!.TryGetStackTrace(out string? stackTrace);
        Assert.True(stackTraceReturned);
        Assert.NotEmpty(stackTrace!);
        Assert.DoesNotContain("InnerException type:", stackTrace);
    }

    [Fact]
    public void LogException_WithInnerStack_IncludeStackTrace_Enabled()
    {
        using var exporter = new TestExporter();
        using var loggerFactory = LoggerFactory.Create(builder => builder
            .AddOpenTelemetryLogging(options => options.IncludeStackTrace = true)
            .AddProcessor(new SimpleLogRecordExportProcessor(exporter)));

        ILogger logger = loggerFactory.CreateLogger("R9");

        LoggerTests.ThrowExceptionAndLogError(() => TestExceptionThrower.ThrowExceptionWithInnerException(), logger);

        bool stackTraceReturned = exporter.FirstState!.TryGetStackTrace(out string? stackTrace);
        Assert.NotEmpty(stackTrace!);
        Assert.Contains("InnerException type:", stackTrace);
    }

    [Fact]
    public void LogException_WithInnerStack_EmptyTopException_IncludeStackTrace_Enabled()
    {
        using var exporter = new TestExporter();
        using var loggerFactory = LoggerFactory.Create(builder => builder
            .AddOpenTelemetryLogging(options => options.IncludeStackTrace = true)
            .AddProcessor(new SimpleLogRecordExportProcessor(exporter)));

        ILogger logger = loggerFactory.CreateLogger("R9");

#pragma warning disable CA1031 // Do not catch general exception types
        try
        {
            TestExceptionThrower.ThrowExceptionWithoutInnerException();
        }
        catch (Exception ex)
        {
            logger.LogError(new NotSupportedException("empty stack trace", innerException: ex), "test exception");
        }
#pragma warning restore CA1031 // Do not catch general exception types

        bool stackTraceReturned = exporter.FirstState!.TryGetStackTrace(out string? stackTrace);
        Assert.StartsWith($"{Environment.NewLine}InnerException type:System.NotSupportedException message:Specified method is not supported", stackTrace);
    }

    [Fact]
    public void LogException_WithMultipleLevelInnerStack_IncludeStackTrace_Enabled()
    {
        using var exporter = new TestExporter();
        using var loggerFactory = LoggerFactory.Create(builder => builder
            .AddOpenTelemetryLogging(options => options.IncludeStackTrace = true)
            .AddProcessor(new SimpleLogRecordExportProcessor(exporter)));

        ILogger logger = loggerFactory.CreateLogger("R9");

        LoggerTests.ThrowExceptionAndLogError(() => TestExceptionThrower.ThrowExceptionWithMultipleLevelInnerException(), logger);

        bool stackTraceReturned = exporter.FirstState!.TryGetStackTrace(out string? stackTrace);
        Assert.NotEmpty(stackTrace!);
        Assert.Contains("InnerException type:System.ArgumentException message:2nd level exception", stackTrace);
        Assert.Contains("InnerException type:System.AggregateException message:Exception caught in Class C", stackTrace);
    }

    [Fact]
    public void LogException_WithMultipleLevelInnerStack_BeyondSupportedLimit_Enabled()
    {
        using var exporter = new TestExporter();
        using var loggerFactory = LoggerFactory.Create(builder => builder
            .AddOpenTelemetryLogging(options => options.IncludeStackTrace = true)
            .AddProcessor(new SimpleLogRecordExportProcessor(exporter)));

        ILogger logger = loggerFactory.CreateLogger("R9");

        LoggerTests.ThrowExceptionAndLogError(() => TestExceptionThrower.ThrowExceptionWithMultipleLevelLargeStack(), logger);

        bool stackTraceReturned = exporter.FirstState!.TryGetStackTrace(out string? stackTrace);
        Assert.NotEmpty(stackTrace!);
        Assert.DoesNotContain("InnerException type:System.AggregateException message:Exception caught in Class C", stackTrace);
    }

    [Fact]
    public void LogException_WithVerylargeStack_StackTraceTruncatedBeyondMaxSupported()
    {
        using var exporter = new TestExporter();
        using var loggerFactory = LoggerFactory.Create(builder => builder
            .AddOpenTelemetryLogging(options => options.IncludeStackTrace = true)
            .AddProcessor(new SimpleLogRecordExportProcessor(exporter)));

        ILogger logger = loggerFactory.CreateLogger("R9");

        LoggerTests.ThrowExceptionAndLogError(() => TestExceptionThrower.ThrowExceptionWithBigExceptionStack(), logger);

        bool stackTraceReturned = exporter.FirstState!.TryGetStackTrace(out string? stackTrace);
        Assert.NotEmpty(stackTrace!);
        Assert.Equal(4096, stackTrace!.Length);
    }

    [Fact]
    public void LogException_NullStack_IncludeStackTrace_Enabled()
    {
        using var exporter = new TestExporter();
        using var loggerFactory = LoggerFactory.Create(builder => builder
            .AddOpenTelemetryLogging(options => options.IncludeStackTrace = true)
            .AddProcessor(new SimpleLogRecordExportProcessor(exporter)));

        ILogger logger = loggerFactory.CreateLogger("R9");
        logger.LogError(new NotImplementedException(), "Method is not implemented");

        bool stackTraceReturned = exporter.FirstState!.TryGetStackTrace(out string? stackTrace);
        Assert.True(stackTraceReturned);
        Assert.Empty(stackTrace!);
    }

    [Fact]
    public void CreateLoggerWithFormattedMessages()
    {
        using var exporter = new TestExporter();
        using var loggerFactory = LoggerFactory.Create(builder => builder
            .AddOpenTelemetryLogging(options => options.UseFormattedMessage = true)
            .AddProcessor(new SimpleLogRecordExportProcessor(exporter)));

        var logMessage = "This is testing {user}";
        var logger = loggerFactory.CreateLogger("R9");
        logger.LogError(logMessage, "testUser");

        Assert.Equal("This is testing testUser", exporter.FirstLogRecord!.FormattedMessage);
    }

    [Fact]
    public void CreateMultipleLoggers()
    {
        using TestExporter exporter = new TestExporter();
        using var firstLoggerFactory = LoggerFactory.Create(builder => builder
            .AddOpenTelemetryLogging()
            .AddProcessor(new SimpleLogRecordExportProcessor(exporter)));

        var logMessage = "This is testing {user}";
        ILogger logger = firstLoggerFactory.CreateLogger("R9");
        ILogger secondLogger = firstLoggerFactory.CreateLogger("R9Test");

        logger.LogError(logMessage, "testUser");

        var dictExpected = new Dictionary<string, object>
        {
            { "{OriginalFormat}", logMessage },
            { "user", "testUser" }
        };

        Assert.True(Helpers.CompareStateValues(exporter.FirstState!, dictExpected));

        secondLogger.LogInformation(logMessage, "testUser2");
        var dictExpected2 = new Dictionary<string, object>
        {
            { "{OriginalFormat}", logMessage },
            { "user", "testUser2" }
        };

        Assert.True(Helpers.CompareStateValues(exporter.FirstState!, dictExpected2));
    }

    [Fact]
    public void CreateLoggerReturnsExistingLoggerWhenExists()
    {
        using var loggerProvider = CreateLoggerProvider();

        ILogger logger = loggerProvider.CreateLogger("R9");
        ILogger secondLogger = loggerProvider.CreateLogger("R9");

        logger.LogInformation("test message");
        Assert.Equal(logger, secondLogger);
    }

    [Fact]
    public void CreateLoggerProviderWithNullOptions()
    {
        Assert.Throws<ArgumentException>(() => CreateLoggerProvider(IOptions.Create<LoggingOptions>(null!)));
    }

    [Fact]
    public void CreateScopes()
    {
        using var loggerProvider = CreateLoggerProvider();

        ILogger logger = loggerProvider.CreateLogger("R9");
        Assert.NotNull(logger);
        loggerProvider.SetScopeProvider(new LoggerExternalScopeProvider());

        using (logger.BeginScope("{ScopeName}", "OuterScope"))
        {
            logger.Log(LogLevel.None, new EventId(1001), "test message", null, null!);
        }

        // Should not throw
        loggerProvider.SetScopeProvider(null!);
        logger.BeginScope("{ScopeName}", "NewScope");
    }

    [Fact]
    public void R9LoggerWithNullAndValidParams()
    {
        using var loggerProvider = CreateLoggerProvider();

        Assert.NotNull(new Logger("categoryName", loggerProvider));

        // Validating that multiple dispose calls are safe
#pragma warning disable S3966 // Objects should not be disposed more than once
        loggerProvider.Dispose();
#pragma warning restore S3966 // Objects should not be disposed more than once
    }

    [Fact]
    public void NullLoggingBuilder()
    {
        ILoggingBuilder? loggingBuilder = null;
        Assert.Throws<ArgumentNullException>(() => loggingBuilder!.AddOpenTelemetryLogging(_ => { }));
    }

#if false
    [Fact]
    public void LoggerWithSimpleProcessorsUsesPropertyBagPool()
    {
        using var exporter = new TestExporter();
        var processors = new List<BaseProcessor<LogRecord>>
            {
                new SimpleLogRecordExportProcessor(exporter),
                new SimpleLogRecordExportProcessor(exporter),
                new SimpleLogRecordExportProcessor(exporter),
                new TestProcessor(),
            };
        var propertyBagPoolMock = new Mock<ObjectPool<LogEnrichmentPropertyBag>>();
        propertyBagPoolMock
            .Setup(o => o.Get())
            .Returns(new LogEnrichmentPropertyBag());
        propertyBagPoolMock
            .Setup(o => o.Return(It.IsAny<LogEnrichmentPropertyBag>()));

        using var loggerProvider = CreateLoggerProvider(processors: processors);
        var logger = new Logger("categoryName", loggerProvider, propertyBagPoolMock.Object);
        var logMessage = "This is testing {user}";
        logger.LogInformation(logMessage, "testUser");

        propertyBagPoolMock.Verify(o => o.Get(), Times.Exactly(1));
        propertyBagPoolMock.Verify(o => o.Return(It.IsAny<LogEnrichmentPropertyBag>()), Times.Exactly(1));
        Assert.NotNull(loggerProvider);
        Assert.True(loggerProvider.CanUsePropertyBagPool);
    }
#endif

    [Fact]
    public void LoggerWithBatchProcessorsDoesntUsePropertyBagPool()
    {
        using var exporter = new TestExporter();
        var processors = new List<BaseProcessor<LogRecord>>
        {
            new BatchLogRecordExportProcessor(exporter),
            new BatchLogRecordExportProcessor(exporter),
            new BatchLogRecordExportProcessor(exporter),
            new TestProcessor(),
        };

        using var loggerProvider = CreateLoggerProvider(processors: processors);

        Assert.NotNull(loggerProvider);
        Assert.False(loggerProvider.CanUsePropertyBagPool);

        var logger = loggerProvider.CreateLogger(Guid.NewGuid().ToString());
        Assert.NotNull(logger);

        logger.LogInformation("test message");
    }

    [Fact]
    public void LoggerWithSimpleAndBatchProcessorsDoesntUsePropertyBagPool()
    {
        using var exporter = new TestExporter();
        var processors = new List<BaseProcessor<LogRecord>>
        {
            new SimpleLogRecordExportProcessor(exporter),
            new BatchLogRecordExportProcessor(exporter),
            new BatchLogRecordExportProcessor(exporter),
            new TestProcessor(),
        };

        using var loggerProvider = CreateLoggerProvider(processors: processors);

        Assert.NotNull(loggerProvider);
        Assert.False(loggerProvider.CanUsePropertyBagPool);
    }

    [Fact]
    public void DependencyInjectionSetup()
    {
        using var host = FakeHost.CreateBuilder(options => options.FakeLogging = false)
            .ConfigureLogging(loggingBuilder => loggingBuilder
                .AddOpenTelemetryLogging(options => options.UseFormattedMessage = true)
                .AddProcessor(new TestProcessor()))
            .ConfigureServices(services => services
                .AddLogEnricher<SimpleEnricher>()
                .Configure<LoggingOptions>(options => options.IncludeScopes = true))
            .Build();

        var loggerProvider = (LoggerProvider)host.Services.GetRequiredService<ILoggerProvider>();
        Assert.Single(loggerProvider.Enrichers);
        Assert.IsType<SimpleEnricher>(loggerProvider.Enrichers[0]);
        Assert.NotNull(loggerProvider.Processor);
        Assert.IsType<TestProcessor>(loggerProvider.Processor);
        Assert.True(loggerProvider.UseFormattedMessage);
        Assert.True(loggerProvider.IncludeScopes);
    }

    [Fact]
    public void AddCustomProcessorType()
    {
        using var host = FakeHost.CreateBuilder(options => options.FakeLogging = false)
            .ConfigureLogging(loggingBuilder => loggingBuilder
                .AddOpenTelemetryLogging()
                .AddProcessor<TestProcessor>())
            .Build();

        var loggerProvider = (LoggerProvider)host.Services.GetRequiredService<ILoggerProvider>();
        Assert.NotNull(loggerProvider.Processor);
        Assert.IsType<TestProcessor>(loggerProvider.Processor);
    }

    [Fact]
    public void AddMultipleCustomProcessorTypes()
    {
        using var host = FakeHost.CreateBuilder(options => options.FakeLogging = false)
            .ConfigureLogging(loggingBuilder => loggingBuilder
                .AddOpenTelemetryLogging()
                .AddProcessor<TestProcessor>()
                .AddProcessor<TestProcessor>())
            .Build();

        var loggerProvider = (LoggerProvider)host.Services.GetRequiredService<ILoggerProvider>();
        var processor = loggerProvider.Processor;
        Assert.NotNull(processor);
        Assert.IsType<CompositeProcessor<LogRecord>>(processor);
    }

    [Fact]
    public void AddCustomProcessorTypeAndInstance()
    {
        using var host = FakeHost.CreateBuilder(options => options.FakeLogging = false)
            .ConfigureLogging(loggingBuilder => loggingBuilder
                .AddOpenTelemetryLogging()
                .AddProcessor<TestProcessor>()
                .AddProcessor(new TestProcessor()))
            .Build();

        var loggerProvider = (LoggerProvider)host.Services.GetRequiredService<ILoggerProvider>();
        var processor = loggerProvider.Processor;
        Assert.NotNull(processor);
        Assert.IsType<CompositeProcessor<LogRecord>>(processor);
    }

    [Fact]
    public void LoggerProviderDisposeLogic()
    {
        var processor = new TestProcessor();
        var loggerProvider = CreateLoggerProvider(processors: new List<BaseProcessor<LogRecord>> { processor });

        loggerProvider.Dispose();
#pragma warning disable S3966 // Objects should not be disposed more than once
        loggerProvider.Dispose();
#pragma warning restore S3966 // Objects should not be disposed more than once

        Assert.Equal(1, processor.DisposeCalledTimes);
    }

    [Fact]
    public void LoggerConfiguredWithConfigurationSection()
    {
        var configRoot = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
        var configSection = configRoot.GetSection("Logging");

        using var host = FakeHost.CreateBuilder(options => options.FakeLogging = false)
            .ConfigureLogging(loggingBuilder => loggingBuilder.AddOpenTelemetryLogging(configSection))
            .Build();

        var loggerProvider = (LoggerProvider)host.Services.GetRequiredService<ILoggerProvider>();
        Assert.True(loggerProvider.UseFormattedMessage);
        Assert.False(loggerProvider.IncludeScopes);
    }

    [Fact]
    public void LogException_IncludeStackTrace_Enabled_StackTraceLengthConfigured_GreaterThanRange_Throw()
    {
        using var exporter = new TestExporter();
        Assert.Throws<OptionsValidationException>(() =>
        {
            using var loggerFactory = LoggerFactory.Create(builder => builder
            .AddOpenTelemetryLogging(options =>
            {
                options.IncludeStackTrace = true;
                options.MaxStackTraceLength = 32770;
            })
            .AddProcessor(new SimpleLogRecordExportProcessor(exporter)));
        });
    }

    [Fact]
    public void LogException_IncludeStackTrace_Enabled_StackTraceLengthConfigured_LessThanRange_Throws()
    {
        using var exporter = new TestExporter();
        Assert.Throws<OptionsValidationException>(() =>
        {
            using var loggerFactory = LoggerFactory.Create(builder => builder
            .AddOpenTelemetryLogging(options =>
            {
                options.IncludeStackTrace = true;
                options.MaxStackTraceLength = 2046;
            })
            .AddProcessor(new SimpleLogRecordExportProcessor(exporter)));
        });
    }

    [Fact]
    public void LogException_IncludeStackTrace_Disabled_StackTraceLengthConfigured_GreaterThanRange_Throws()
    {
        using var exporter = new TestExporter();
        Assert.Throws<OptionsValidationException>(() =>
        {
            using var loggerFactory = LoggerFactory.Create(builder => builder
            .AddOpenTelemetryLogging(options =>
            {
                options.MaxStackTraceLength = 32770;
            })
            .AddProcessor(new SimpleLogRecordExportProcessor(exporter)));
        });
    }

    [Fact]
    public void LogException_IncludeStackTrace_Disabled_StackTraceLengthConfigured_LessThanRange_Throws()
    {
        using var exporter = new TestExporter();
        Assert.Throws<OptionsValidationException>(() =>
        {
            using var loggerFactory = LoggerFactory.Create(builder => builder
            .AddOpenTelemetryLogging(options =>
            {
                options.MaxStackTraceLength = 2047;
            })
            .AddProcessor(new SimpleLogRecordExportProcessor(exporter)));
        });
    }

    [Fact]
    public void LogException_IncludeStackTrace_Disabled_StackTraceLengthConfigured_NoStackTrace()
    {
        using var exporter = new TestExporter();
        using var loggerFactory = LoggerFactory.Create(builder => builder
            .AddOpenTelemetryLogging(options =>
            {
                options.MaxStackTraceLength = 4000;
            })
            .AddProcessor(new SimpleLogRecordExportProcessor(exporter)));

        ILogger logger = loggerFactory.CreateLogger("R9");
        logger.LogError(new NotImplementedException(), "Method is not implemented");

        Assert.False(exporter.FirstState!.TryGetStackTrace(out string? stackTrace));
    }

    [Fact]
    public void LogException_MultipleLevelInnerStack_BeyondSupportedLimit_MaxStackTraceLength()
    {
        using var exporter = new TestExporter();
        using var loggerFactory = LoggerFactory.Create(builder => builder
            .AddOpenTelemetryLogging(options =>
            {
                options.IncludeStackTrace = true;
                options.MaxStackTraceLength = 32768;
            })
            .AddProcessor(new SimpleLogRecordExportProcessor(exporter)));

        ILogger logger = loggerFactory.CreateLogger("R9");

        LoggerTests.ThrowExceptionAndLogError(() => TestExceptionThrower.ThrowExceptionWithMultipleLevelLargeStack(), logger);

        bool stackTraceReturned = exporter.FirstState!.TryGetStackTrace(out string? stackTrace);
        Assert.NotEmpty(stackTrace!);
        Assert.DoesNotContain("InnerException type:System.AggregateException message:Exception caught in Class C", stackTrace);
    }

    [Fact]
    public void LogException_VerylargeStack_StackTraceTruncated_MaxStackTraceLength()
    {
        using var exporter = new TestExporter();
        using var loggerFactory = LoggerFactory.Create(builder => builder
            .AddOpenTelemetryLogging(options =>
            {
                options.IncludeStackTrace = true;
                options.MaxStackTraceLength = 32768;
            })
            .AddProcessor(new SimpleLogRecordExportProcessor(exporter)));

        ILogger logger = loggerFactory.CreateLogger("R9");

        LoggerTests.ThrowExceptionAndLogError(() => TestExceptionThrower.ThrowExceptionWithBigExceptionStack(), logger);

        bool stackTraceReturned = exporter.FirstState!.TryGetStackTrace(out string? stackTrace);
        Assert.NotEmpty(stackTrace!);
        Assert.Equal(32768, stackTrace!.Length);
    }

    [Fact]
    public void LoggerProvider_AddFilter_ShouldApplyFilter()
    {
        using var exporter = new TestExporter();
        var logMessage = "This is testing {user}";

        var hostBuilder = FakeHost.CreateBuilder(options => options.FakeLogging = false)
            .ConfigureLogging(builder =>
            {
                _ = builder.AddOpenTelemetryLogging().AddProcessor(new SimpleLogRecordExportProcessor(exporter));
                _ = builder.AddFilter<LoggerProvider>(level => level == LogLevel.Warning);
            });

        var host = hostBuilder.Build();
        var logger = host.Services.GetRequiredService<ILogger<LogEnrichmentTests>>();

        var dictExpected = new Dictionary<string, object>
            {
                { "{OriginalFormat}", logMessage },
                { "user", "userWarning" }
            };

        logger.LogError(logMessage, "userError");
        Assert.Null(exporter.FirstState);

        logger.LogInformation(logMessage, "userInfo");
        Assert.Null(exporter.FirstState);

        logger.LogDebug(logMessage, "userDebug");
        Assert.Null(exporter.FirstState);

        logger.LogWarning(logMessage, "userWarning");
        Assert.True(Helpers.CompareStateValues(exporter.FirstState!, dictExpected));
    }

    private static void ThrowExceptionAndLogError(Action action, ILogger logger)
    {
#pragma warning disable CA1031 // Do not catch general exception types
        try
        {
            action();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "test exception");
        }
#pragma warning restore CA1031 // Do not catch general exception types
    }
}
