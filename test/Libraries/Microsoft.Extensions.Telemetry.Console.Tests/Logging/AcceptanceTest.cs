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
        CaptureAndRecoverConsoleOut(() =>
        {
            using var writer = new StringWriter();
            System.Console.SetOut(writer);
            var logger = GetLogger();
            var logMessage = "Refrigerator is broken.";

            logger.LogInformation(logMessage);

            var logs = RemoveColors(writer.ToString());
            Assert.EndsWith(GetExpectedMessage(logMessage), logs);
        });
    }

    [Fact]
    public void StructuredLogExportedCorrectly()
    {
        CaptureAndRecoverConsoleOut(() =>
        {
            using var writer = new StringWriter();
            System.Console.SetOut(writer);
            var logger = GetLogger();
            var logMessage = "{name} is broken.";

            logger.LogInformation(logMessage, "Refrigerator");

            var logs = RemoveColors(writer.ToString());
            Assert.EndsWith(GetExpectedMessage(logMessage), logs);
        });
    }

    [Fact]
    public void StructuredLogWithUseFormattedMessageExportedCorrectly()
    {
        CaptureAndRecoverConsoleOut(() =>
        {
            using var writer = new StringWriter();
            System.Console.SetOut(writer);
            var logger = GetLogger(useFormattedMessage: true);
            var logMessage = "{name} is broken.";
            var name = "Refrigerator";
            var formattedMessage = $"{name} is broken.";

            logger.LogInformation(logMessage, name);

            var logs = RemoveColors(writer.ToString());
            Assert.EndsWith(GetExpectedMessage(formattedMessage), logs);
        });
    }

    [Theory]
    [CombinatorialData]
    public void LogWithScopesExportedCorrectly(bool useFormattedMessage, bool useEnricher)
    {
        CaptureAndRecoverConsoleOut(() =>
        {
            using var writer = new StringWriter();
            System.Console.SetOut(writer);
            ILogEnricher? enricher = useEnricher ? new TestLogEnricher() : null;
            var logger = GetLogger(includeScopes: true, useFormattedMessage, enricher);
            var logMessage = "{name} is broken.";
            var name = "Refrigerator";
            var formattedMessage = $"{name} is broken.";
            var scope1 = "operation";
            var scope2 = "hardware";

            using (logger.BeginScope(scope1))
            using (logger.BeginScope(scope2))
            {
                logger.LogInformation(logMessage, name);
            }

            var logs = RemoveColors(writer.ToString());

            var enricherScope = useEnricher ? $" {TestLogEnricher.Key}:{TestLogEnricher.Value}" : string.Empty;
            Assert.StartsWith($"Scope: {scope1} {scope2} name:{name}{enricherScope}{Environment.NewLine}", logs);

            string message = useFormattedMessage ? formattedMessage : logMessage;
            Assert.EndsWith(GetExpectedMessage(message), logs);
        });
    }

    [Fact]
    public void ExceptionLogExportedCorrectly()
    {
        CaptureAndRecoverConsoleOut(() =>
        {
            using var writer = new StringWriter();
            System.Console.SetOut(writer);
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

            var logs = RemoveColors(writer.ToString());
            Assert.Contains(GetExpectedMessage(logMessage), logs);
        });
    }

    [Fact]
    public void LogWithTraceIdExportedCorrectly()
    {
        CaptureAndRecoverConsoleOut(() =>
        {
            using var writer = new StringWriter();
            System.Console.SetOut(writer);
            var logger = GetLogger();
            var logMessage = "Refrigerator is broken.";

            logger.LogInformation(logMessage);

            var logs = RemoveColors(writer.ToString());
            Assert.EndsWith(GetExpectedMessage(logMessage), logs);
        });
    }

    [Fact]
    public void GetOriginalFormat_GivenNullOrNothingForOriginalFormat_ReturnsEmptyString()
    {
        var methodInfo = typeof(LoggingConsoleExporter).GetMethod("GetOriginalFormat", BindingFlags.NonPublic | BindingFlags.Static);
#if NET5_0_OR_GREATER
        using var consoleLogExporter = new LoggingConsoleExporter(MSOptions.Create(new LoggingConsoleOptions()));
#else
        using var consoleLogExporter = new LoggingConsoleExporter();
#endif

        ReadOnlyCollection<KeyValuePair<string, object>> state = new(new List<KeyValuePair<string, object>> { new("{OriginalFormat}", null!) });
        object[] parameters = { state };
        var result = methodInfo?.Invoke(consoleLogExporter, parameters);
        Assert.IsType<string>(result);
        Assert.Equal(string.Empty, result);

        state = new(new List<KeyValuePair<string, object>>());
        parameters[0] = state;
        result = methodInfo?.Invoke(consoleLogExporter, parameters);
        Assert.IsType<string>(result);
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void GetOriginalFormat_GivenNullStateForOriginalFormat_ReturnsEmptyString()
    {
        var methodInfo = typeof(LoggingConsoleExporter).GetMethod("GetOriginalFormat", BindingFlags.NonPublic | BindingFlags.Static);
#if NET5_0_OR_GREATER
        using var consoleLogExporter = new LoggingConsoleExporter(MSOptions.Create(new LoggingConsoleOptions()));
#else
        using var consoleLogExporter = new LoggingConsoleExporter();
#endif

        ReadOnlyCollection<KeyValuePair<string, object>>? state = null;
        object?[] parameters = { state };
        var result = methodInfo?.Invoke(consoleLogExporter, parameters);
        Assert.IsType<string>(result);
        Assert.Equal(string.Empty, result);
    }

    private static ILogger GetLogger(bool includeScopes = false, bool useFormattedMessage = false, ILogEnricher? enricher = null)
    {
        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            _ = builder
                .AddOpenTelemetryLogging(options =>
                {
                    options.IncludeScopes = includeScopes;
                    options.UseFormattedMessage = useFormattedMessage;
                })
                .AddConsoleExporter();

            if (enricher is not null)
            {
                _ = builder.Services.AddLogEnricher(enricher);
            }
        });

        return loggerFactory.CreateLogger<AcceptanceTest>();
    }

    private static void CaptureAndRecoverConsoleOut(Action test)
    {
        var consoleOut = System.Console.Out;

        try
        {
            test();
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
