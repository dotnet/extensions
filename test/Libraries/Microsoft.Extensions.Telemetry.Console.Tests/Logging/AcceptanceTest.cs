// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Telemetry.Console.Internal.Test;
using Microsoft.Extensions.Telemetry.Enrichment;
using Microsoft.Extensions.Telemetry.Logging;
using Xunit;

#if NET5_0_OR_GREATER
using Microsoft.Extensions.Telemetry.Console.Internal;
using MSOptions = Microsoft.Extensions.Options.Options;
#endif

namespace Microsoft.Extensions.Telemetry.Console.Test;

[Collection("StdoutUsage")]
#if NET7_0_OR_GREATER
public partial class AcceptanceTest
{
    [GeneratedRegex("\u001b.*?m", RegexOptions.Compiled)]
    private static partial Regex ColorsRegex();

    private static readonly Regex _removeColorsRegex = ColorsRegex();
#else
public class AcceptanceTest
{
    private static readonly Regex _removeColorsRegex = new("\x1B.*?m", RegexOptions.Compiled);
#endif

    [Fact]
    public void UnstructuredLogExportedCorrectly()
    {
        CaptureAndRecoverConsoleOut(consoleOut =>
        {
            var logger = GetLogger();
            var logMessage = "Refrigerator is broken.";

            logger.LogInformation(logMessage);

            var logs = RemoveColors(consoleOut.ToString());
            Assert.EndsWith(GetExpectedMessage(logMessage), logs);
        });
    }

    [Fact]
    public void StructuredLogExportedCorrectly()
    {
        CaptureAndRecoverConsoleOut(consoleOut =>
        {
            var logger = GetLogger();
            var logMessage = "{name} is broken.";

            logger.LogInformation(logMessage, "Refrigerator");

            var logs = RemoveColors(consoleOut.ToString());
            Assert.Contains(GetExpectedMessage(logMessage), logs);
        });
    }

    [Fact]
    public void StructuredLogWithUseFormattedMessageExportedCorrectly()
    {
        CaptureAndRecoverConsoleOut(consoleOut =>
        {
            var logger = GetLogger(useFormattedMessage: true);
            var logMessage = "{name} is broken.";
            var name = "Refrigerator";
            var formattedMessage = $"{name} is broken.";

            logger.LogInformation(logMessage, name);

            var logs = RemoveColors(consoleOut.ToString());
            Assert.Contains(GetExpectedMessage(formattedMessage), logs);
        });
    }

    [Theory]
    [CombinatorialData]
    public void LogWithScopesExportedCorrectly(bool includeLoggingScopes, bool includeExporterScopes)
    {
        CaptureAndRecoverConsoleOut(consoleOut =>
        {
            var logger = GetLogger(
                includeLoggingScopes: includeLoggingScopes,
                includeExporterScopes: includeExporterScopes);

            var logMessage = "Refrigerator is broken.";
            var scope1 = new KeyValuePair<string, object>("operation_name", "operation");
            var scope2 = "hardware";

            using (logger.BeginScope(new[] { scope1 }))
            using (logger.BeginScope(scope2))
            {
                logger.LogInformation(logMessage);
            }

            var logs = RemoveColors(consoleOut.ToString());
            var expectedMessage = GetExpectedMessage(logMessage);

            if (includeLoggingScopes)
            {
#if NET5_0_OR_GREATER
                if (includeExporterScopes)
                {
                    Assert.StartsWith($"Scope: {scope1.Key}:{scope1.Value} {scope2}{Environment.NewLine}", logs);
                }
                else
                {
                    Assert.DoesNotContain("Scope:", logs);
                }
#else
                Assert.StartsWith($"Scope: {scope1.Key}:{scope1.Value} {scope2}{Environment.NewLine}", logs);
#endif
            }
            else
            {
                Assert.DoesNotContain("Scope:", logs);
            }

            Assert.EndsWith(expectedMessage, logs);
        });
    }

    [Theory]
    [CombinatorialData]
    public void LogsWithDimensionsExportedCorrectly(bool includeDimensions)
    {
        CaptureAndRecoverConsoleOut(consoleOut =>
        {
            var logger = GetLogger(
                includeDimensions: includeDimensions,
                useFormattedMessage: false,
                enricher: new TestLogEnricher());

            var logMessage = "{name} is broken.";
            var name = "Refrigerator";
            var nameParameter = $"name={name}";
            var enrichmentProperty = $"{TestLogEnricher.Key}={TestLogEnricher.Value}";

            logger.LogInformation(logMessage, name);

            var logs = RemoveColors(consoleOut.ToString());
            var expectedMessage = GetExpectedMessage(logMessage);

#if NET5_0_OR_GREATER
            if (includeDimensions)
            {
                Assert.Contains(nameParameter, logs);
                Assert.Contains(enrichmentProperty, logs);
                Assert.Contains(expectedMessage, logs);
            }
            else
            {
                Assert.DoesNotContain(nameParameter, logs);
                Assert.DoesNotContain(enrichmentProperty, logs);
                Assert.EndsWith(expectedMessage, logs);
            }
#else
            Assert.Contains(nameParameter, logs);
            Assert.Contains(enrichmentProperty, logs);
            Assert.Contains(expectedMessage, logs);
#endif
        });
    }

    [Fact]
    public void ExceptionLogExportedCorrectly()
    {
        CaptureAndRecoverConsoleOut(consoleOut =>
        {
            var logger = GetLogger();
            var logMessage = "Logging message for {reason}.";
            try
            {
                throw new AggregateException("Aggregate exception message",
                    new DivideByZeroException("Divide by zero exception message", new IOException("IO exception message")),
                    new ArgumentNullException("Parameter name"));
            }
            catch (AggregateException ex)
            {
                logger.LogInformation(ex, logMessage, "testing");
            }

            var logs = RemoveColors(consoleOut.ToString());
            Assert.Contains(GetExpectedMessage(logMessage), logs);
        });
    }

    [Fact]
    public void LogWithTraceIdExportedCorrectly()
    {
        CaptureAndRecoverConsoleOut(consoleOut =>
        {
            var logger = GetLogger();
            var logMessage = "Refrigerator is broken.";

            logger.LogInformation(logMessage);

            var logs = RemoveColors(consoleOut.ToString());
            Assert.EndsWith(GetExpectedMessage(logMessage), logs);
        });
    }

    [Fact]
    public void GetOriginalFormat_GivenNullOrNothingForOriginalFormat_ReturnsEmptyString()
    {
        var methodInfo = typeof(LoggingConsoleExporter).GetMethod("GetOriginalFormat", BindingFlags.NonPublic | BindingFlags.Static);
#if NET5_0_OR_GREATER
        using var loggingConsoleExporter = new LoggingConsoleExporter(MSOptions.Create(new LoggingConsoleOptions()));
#else
        using var loggingConsoleExporter = new LoggingConsoleExporter();
#endif

        ReadOnlyCollection<KeyValuePair<string, object>> state = new(new List<KeyValuePair<string, object>> { new("{OriginalFormat}", null!) });
        object[] parameters = { state };
        var result = methodInfo?.Invoke(loggingConsoleExporter, parameters);
        Assert.IsType<string>(result);
        Assert.Equal(string.Empty, result);

        state = new(new List<KeyValuePair<string, object>>());
        parameters[0] = state;
        result = methodInfo?.Invoke(loggingConsoleExporter, parameters);
        Assert.IsType<string>(result);
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void GetOriginalFormat_GivenNullStateForOriginalFormat_ReturnsEmptyString()
    {
        var methodInfo = typeof(LoggingConsoleExporter).GetMethod("GetOriginalFormat", BindingFlags.NonPublic | BindingFlags.Static);
#if NET5_0_OR_GREATER
        using var loggingConsoleExporter = new LoggingConsoleExporter(MSOptions.Create(new LoggingConsoleOptions()));
#else
        using var loggingConsoleExporter = new LoggingConsoleExporter();
#endif

        ReadOnlyCollection<KeyValuePair<string, object>>? state = null;
        object?[] parameters = { state };
        var result = methodInfo?.Invoke(loggingConsoleExporter, parameters);
        Assert.IsType<string>(result);
        Assert.Equal(string.Empty, result);
    }

    private static ILogger GetLogger(
        bool includeLoggingScopes = false,
        bool includeExporterScopes = false,
        bool includeDimensions = false,
        bool useFormattedMessage = false,
        ILogEnricher? enricher = null)
    {
        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            _ = builder
                .AddOpenTelemetryLogging(options =>
                {
                    options.IncludeScopes = includeLoggingScopes;
                    options.UseFormattedMessage = useFormattedMessage;
                })
#if NET5_0_OR_GREATER
                .AddConsoleExporter(options =>
                {
                    options.IncludeScopes = includeExporterScopes;
                    options.IncludeDimensions = includeDimensions;
                });
#else
                .AddConsoleExporter();
#endif

            if (enricher is not null)
            {
                _ = builder.Services.AddLogEnricher(enricher);
            }
        });

        return loggerFactory.CreateLogger<AcceptanceTest>();
    }

    private static void CaptureAndRecoverConsoleOut(Action<StringWriter> test)
    {
        var consoleOut = System.Console.Out;
        using var newConsoleOut = new StringWriter();
        System.Console.SetOut(newConsoleOut);

        try
        {
            test(newConsoleOut);
        }
        finally
        {
            System.Console.SetOut(consoleOut);
        }
    }

    private static string RemoveColors(string message)
    {
        return _removeColorsRegex.Replace(message, string.Empty);
    }

    private static string GetExpectedMessage(string logMessage)
    {
#if NET5_0_OR_GREATER
        return $"({LogLevel.Information.InShortString()}) {default(ActivityTraceId)} {default(ActivitySpanId)} {logMessage} ({typeof(AcceptanceTest).FullName}/0){Environment.NewLine}";
#else
        return $"{LogLevel.Information} {default(ActivityTraceId)} {default(ActivitySpanId)} {logMessage} {typeof(AcceptanceTest).FullName}/0{Environment.NewLine}";
#endif
    }
}
