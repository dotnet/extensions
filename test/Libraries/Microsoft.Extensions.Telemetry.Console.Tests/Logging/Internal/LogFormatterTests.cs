// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if NET5_0_OR_GREATER
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Telemetry.Console.Test.Helpers;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace Microsoft.Extensions.Telemetry.Console.Internal.Test;

[SuppressMessage("Usage", "CA2201:Do not raise reserved exception types", Justification = "Test code")]
public class LogFormatterTests
{
    private static readonly string _newLine = Environment.NewLine;
    private static readonly Regex _regexToDetectTimestamp = new(@"\d\d\d\d-\d\d-\d\d \d\d:\d\d:\d\d\.\d\d\d.*", RegexOptions.Compiled);

    private static void MockClock(LogFormatter formatter)
    {
        formatter.TimeProvider = new FakeTimeProvider(new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void Ctor_ThrowsWhenAnyArgumentsValueIsNull()
    {
        Assert.Throws<ArgumentException>(() => new LogFormatter(
            Microsoft.Extensions.Options.Options.Create(new LogFormatterOptions()),
            Microsoft.Extensions.Options.Options.Create<LogFormatterTheme>(null!)));

        Assert.Throws<ArgumentException>(() => new LogFormatter(
            Microsoft.Extensions.Options.Options.Create<LogFormatterOptions>(null!),
            Microsoft.Extensions.Options.Options.Create(new LogFormatterTheme())));
    }

    [Fact]
    public void Write_WhenScopeIsNullThrows()
    {
        var theme = CreateNoneColorfulLogFormatterTheme();
        var options = CreateAllLightUpOptions();

        using var formatter = new LogFormatter(options, theme);
        using var writer = new StringWriter();

        Assert.Throws<ArgumentNullException>(() =>
            formatter.Write(default, null!, writer));
    }

    [Fact]
    public void WriteScopes_WhenEmpty_ProducesEmptyString()
    {
        var theme = CreateNoneColorfulLogFormatterTheme();
        var options = CreateAllLightUpOptions();

        using var formatter = new LogFormatter(options, theme);

        using var writer = new StringWriter();
        var scopes = new LoggerExternalScopeProvider();

        formatter.WriteScopes(writer, scopes);

        Assert.Equal(string.Empty, writer.ToString());
    }

    [Fact]
    public void WriteScopes_WhenSingleScope_ProducesCorrectOutput()
    {
        var theme = CreateNoneColorfulLogFormatterTheme();
        var options = CreateAllLightUpOptions();

        using var formatter = new LogFormatter(options, theme);

        using var writer = new StringWriter();
        var scopes = new LoggerExternalScopeProvider();

        scopes.Push("key1=value1");

        formatter.WriteScopes(writer, scopes);

        var m = _newLine + "key1=value1";
        Assert.Equal(m, writer.ToString());
    }

    [Fact]
    public void WriteScopes_WhenMultipleScopes_ProducesCorrectOutput()
    {
        var theme = CreateNoneColorfulLogFormatterTheme();
        var options = CreateAllLightUpOptions();

        using var formatter = new LogFormatter(options, theme);

        using var writer = new StringWriter();
        var scopes = new LoggerExternalScopeProvider();

        scopes.Push("key1=value1");
        scopes.Push("key2=value2");

        formatter.WriteScopes(writer, scopes);

        var text = _newLine + "key1=value1 key2=value2";
        Assert.Equal(text, writer.ToString());
    }

    [Fact]
    public void WriteException_WhenNull_DoesNothing()
    {
        var theme = CreateNoneColorfulLogFormatterTheme();
        var options = CreateAllLightUpOptions();

        using var formatter = new LogFormatter(options, theme);

        using var writer = new StringWriter();

        formatter.WriteException(writer, null);

        Assert.Equal(string.Empty, writer.ToString());
    }

    [Fact]
    public void WriteException_WhenSingleException_ProducesCorrectOutput()
    {
        var theme = CreateNoneColorfulLogFormatterTheme();
        var options = CreateAllLightUpOptions();

        using var formatter = new LogFormatter(options, theme);

        using var writer = new StringWriter();
        var exception = new Exception("My flowers are beautiful");

        formatter.WriteException(writer, exception);

        var text = _newLine + "0: Exception: My flowers are beautiful (System.Exception)";
        Assert.Equal(text, writer.ToString());
    }

    [Fact]
    public void WriteException_WithMultipleInnerException_ProducesCorrectOutput()
    {
        var theme = CreateNoneColorfulLogFormatterTheme();
        var options = CreateAllLightUpOptions();

        using var formatter = new LogFormatter(options, theme);

        using var writer = new StringWriter();

        var exception =
            new Exception("I am top level",
                new FormatException("I am the first inner",
                    new ArgumentException("I am an inner squared")));

        formatter.WriteException(writer, exception);

        var text =
            _newLine + "0: Exception: I am top level (System.Exception)" +
            _newLine + "0:0: Exception: I am the first inner (System.FormatException)" +
            _newLine + "0:0:0: Exception: I am an inner squared (System.ArgumentException)";

        Assert.Equal(text, writer.ToString());
    }

    [Fact]
    public void WriteException_WithAggregatedExceptions_ProducesCorrectOutput()
    {
        var theme = CreateNoneColorfulLogFormatterTheme();
        var options = CreateAllLightUpOptions();

        using var formatter = new LogFormatter(options, theme);

        using var writer = new StringWriter();

        var exception =
            new AggregateException(new List<Exception>(2)
            {
                    new FormatException("I am the first aggregated"),
                    new FormatException("I am the second aggregated")
            });

        formatter.WriteException(writer, exception);

        var text =
            _newLine + "0:0: Exception: I am the first aggregated (System.FormatException)" +
            _newLine + "0:1: Exception: I am the second aggregated (System.FormatException)";

        Assert.EndsWith(text, writer.ToString(), StringComparison.CurrentCulture);
    }

    [Fact]
    public void WriteException_WhenStackTracePresent_ProducesCorrectOutput()
    {
        var theme = CreateNoneColorfulLogFormatterTheme();
        var options = CreateAllLightUpOptions();

        using var formatter = new LogFormatter(options, theme);

        using var writer = new StringWriter();
        var exception = new TestException("My flowers are beautiful",
            "--- Lorem ipsum dolor sit amet, consectetur adipiscing elit.");

        formatter.WriteException(writer, exception);

        var text =
            _newLine + "0: Exception: My flowers are beautiful (" + typeof(TestException).FullName + ")" +
            _newLine + "--- Lorem ipsum dolor sit amet, consectetur adipiscing elit.";

        Assert.Equal(text, writer.ToString());
    }

    [Fact]
    public void WriteException_WhenStackTracePresentAndDisabled_DoesNotPrint()
    {
        var theme = CreateNoneColorfulLogFormatterTheme();
        var options = Microsoft.Extensions.Options.Options.Create(new LogFormatterOptions
        {
            IncludeExceptionStacktrace = false
        });

        using var formatter = new LogFormatter(options, theme);

        using var writer = new StringWriter();
        var exception = new TestException("My flowers are beautiful",
            "--- Lorem ipsum dolor sit amet, consectetur adipiscing elit.");

        formatter.WriteException(writer, exception);

        var text =
            _newLine + "0: Exception: My flowers are beautiful (" + typeof(TestException).FullName + ")";

        Assert.Equal(text, writer.ToString());
    }

    [Fact]
    public void Write_WhenDefaultFormatterOptions_IncludesTimestamp()
    {
        var theme = CreateNoneColorfulLogFormatterTheme();
        var options = CreateAllLightUpOptions();

        using var formatter = new LogFormatter(options, theme);
        MockClock(formatter);

        var scopes = new LoggerExternalScopeProvider();
        using var writer = new StringWriter();
        var entry = CreateLogEntry();

        formatter.Write(entry, scopes, writer);

        Assert.Matches(_regexToDetectTimestamp, $"{writer}");
    }

    [Fact]
    public void Write_WhenLogEntryIsNull_IncludesTimestamp()
    {
        var theme = CreateNoneColorfulLogFormatterTheme();
        var options = CreateAllLightUpOptions();

        using var formatter = new LogFormatter(options, theme);
        MockClock(formatter);

        var scopes = new LoggerExternalScopeProvider();
        using var writer = new StringWriter();

        formatter.Write(default, scopes, writer);
        Assert.Matches(_regexToDetectTimestamp, $"{writer}");
    }

    [Fact]
    public void Write_WhenIncludeTimestampDisabled_OmitsTimestamp()
    {
        var theme = CreateNoneColorfulLogFormatterTheme();
        var options = Microsoft.Extensions.Options.Options.Create(new LogFormatterOptions
        {
            IncludeTimestamp = false
        });

        using var formatter = new LogFormatter(options, theme);

        var scopes = new LoggerExternalScopeProvider();
        using var writer = new StringWriter();
        var entry = CreateLogEntry();

        formatter.Write(entry, scopes, writer);

        var text = $"(info) {entry.State.TraceId} {entry.State.SpanId} Message (Category/0)" + _newLine;

        Assert.Equal(text, writer.ToString());
    }

    [Fact]
    public void Write_WhenIncludeSpanIdDisabled_OmitsSpanId()
    {
        var theme = CreateNoneColorfulLogFormatterTheme();
        var options = Microsoft.Extensions.Options.Options.Create(new LogFormatterOptions
        {
            IncludeSpanId = false
        });

        using var formatter = new LogFormatter(options, theme);

        var scopes = new LoggerExternalScopeProvider();
        using var writer = new StringWriter();
        var entry = CreateLogEntry();

        formatter.Write(entry, scopes, writer);

        var text = $"(info) {entry.State.TraceId} Message (Category/0)" + _newLine;

        Assert.EndsWith(text, writer.ToString());
    }

    [Fact]
    public void Write_WhenIncludeTraceIdDisabled_OmitsTraceId()
    {
        var theme = CreateNoneColorfulLogFormatterTheme();
        var options = Microsoft.Extensions.Options.Options.Create(new LogFormatterOptions
        {
            IncludeTraceId = false
        });

        using var formatter = new LogFormatter(options, theme);

        var scopes = new LoggerExternalScopeProvider();
        using var writer = new StringWriter();
        var entry = CreateLogEntry();

        formatter.Write(entry, scopes, writer);

        var text = $"(info) {entry.State.SpanId} Message (Category/0)" + _newLine;

        Assert.EndsWith(text, writer.ToString());
    }

    [Fact]
    public void Write_WhenIncludeLogLevelDisabled_OmitsLogLevel()
    {
        var theme = CreateNoneColorfulLogFormatterTheme();
        var options = Microsoft.Extensions.Options.Options.Create(new LogFormatterOptions
        {
            IncludeLogLevel = false
        });

        using var formatter = new LogFormatter(options, theme);
        MockClock(formatter);

        var scopes = new LoggerExternalScopeProvider();
        using var writer = new StringWriter();
        var entry = CreateLogEntry();

        formatter.Write(entry, scopes, writer);

        Assert.DoesNotContain("[info]", writer.ToString(), StringComparison.CurrentCulture);
        Assert.EndsWith(_newLine, writer.ToString(), StringComparison.CurrentCulture);
    }

    [Fact]
    public void Write_WhenIncludeCategoryDisabled_OmitsCategory()
    {
        var theme = CreateNoneColorfulLogFormatterTheme();
        var options = Microsoft.Extensions.Options.Options.Create(new LogFormatterOptions
        {
            IncludeCategory = false
        });

        using var formatter = new LogFormatter(options, theme);
        MockClock(formatter);

        var scopes = new LoggerExternalScopeProvider();
        using var writer = new StringWriter();
        var entry = CreateLogEntry();

        formatter.Write(entry, scopes, writer);

        Assert.DoesNotContain("(Category/0)", writer.ToString(), StringComparison.CurrentCulture);
        Assert.EndsWith(_newLine, writer.ToString(), StringComparison.CurrentCulture);
    }

    [Fact]
    public void Write_WhenScopeExists_ProducesCorrectOutput()
    {
        var theme = CreateNoneColorfulLogFormatterTheme();
        var options = CreateAllLightUpOptions();

        using var formatter = new LogFormatter(options, theme);
        MockClock(formatter);

        var scopes = new LoggerExternalScopeProvider();

        scopes.Push("Key1=Value1");
        scopes.Push("Key2=Value2");

        using var writer = new StringWriter();
        var entry = CreateLogEntry();

        formatter.Write(entry, scopes, writer);

        var text = _newLine + "Key1=Value1 Key2=Value2" + _newLine;

        Assert.StartsWith(text, writer.ToString(), StringComparison.CurrentCulture);
    }

    [Fact]
    public void Write_WhenExceptionPassed_ProducesCorrectOutput()
    {
        var theme = CreateNoneColorfulLogFormatterTheme();
        var options = CreateAllLightUpOptions();

        using var formatter = new LogFormatter(options, theme);
        MockClock(formatter);

        var scopes = new LoggerExternalScopeProvider();
        using var writer = new StringWriter();
        var entry = CreateLogEntry(e: new Exception("Test"));

        formatter.Write(entry, scopes, writer);

        var text = $"(info) {entry.State.TraceId} {entry.State.SpanId} Message (Category/0)" + _newLine
                                                 + _newLine +
                   "0: Exception: Test (System.Exception)"
                                                 + _newLine;

        Assert.EndsWith(text, writer.ToString(), StringComparison.CurrentCulture);
    }

    [Fact]
    public void WriteDateTime_WhenCalled_UsesFormatFromOptions()
    {
        var theme = CreateNoneColorfulLogFormatterTheme();
        var options = Microsoft.Extensions.Options.Options.Create(new LogFormatterOptions
        {
            TimestampFormat = "HH:mm"
        });

        using var formatter = new LogFormatter(options, theme);
        MockClock(formatter);

        using var writer = new StringWriter();

        formatter.WriteTimestamp(writer);

        Assert.Matches("[0-9][0-9]:[0-9][0-9]", writer.ToString());
    }

    [Fact]
    public void WriteCategory_WhenCalled_WritesCategory()
    {
        var theme = CreateNoneColorfulLogFormatterTheme();
        var options = CreateAllLightUpOptions();

        using var formatter = new LogFormatter(options, theme);

        using var writer = new StringWriter();

        formatter.WriteCategory(writer, "Category", 99);
        Assert.Equal("(Category/99)", writer.ToString());
    }

    [Fact]
    public void Dispose_WhenCalled_DoesNothing()
    {
        var theme = CreateNoneColorfulLogFormatterTheme();
        var options = CreateAllLightUpOptions();

        using var formatter = new LogFormatter(options, theme);
        var exception = Record.Exception(() => formatter.Dispose());

        Assert.Null(exception);
    }

    [Fact]
    public void WriteLogLevel_WhenEnabledColors_WritesCorrectly()
    {
        var options = CreateAllLightUpOptions();
        var theme = CreateNoneColorfulLogFormatterTheme();
        theme.Value.ColorsEnabled = true;

        using var formatter = new LogFormatter(options, theme);

        using var writer = new StringWriter();

        formatter.WriteLogLevel(writer, LogLevel.Warning);
        Assert.Equal("[1m[33m[40m(warn)[49m[39m[22m ", writer.ToString());
    }

    private static IOptions<LogFormatterOptions> CreateAllLightUpOptions()
    {
        return Microsoft.Extensions.Options.Options.Create(new LogFormatterOptions
        {
            IncludeCategory = true,
            IncludeExceptionStacktrace = true,
            IncludeLogLevel = true,
            IncludeScopes = true,
            IncludeTimestamp = true,
            TimestampFormat = "yyyy-MM-dd HH:mm:ss.fff",
            UseUtcTimestamp = true
        });
    }

    private static IOptions<LogFormatterTheme> CreateNoneColorfulLogFormatterTheme()
    {
        return Microsoft.Extensions.Options.Options.Create(new LogFormatterTheme
        {
            ColorsEnabled = false,
            ExceptionStackTrace = Colors.None,
            Exception = Colors.None,
            Dimmed = Colors.None
        });
    }

    private static LogEntry<LogEntryCompositeState> CreateLogEntry(Action<LogEntry<LogEntryCompositeState>>? amend = null, Exception? e = null)
    {
        static string Formatter(LogEntryCompositeState s, Exception? exception) => "Message";
        LogEntryCompositeState state = new LogEntryCompositeState(null, ActivityTraceId.CreateRandom(), ActivitySpanId.CreateRandom());

        var logEntry = new LogEntry<LogEntryCompositeState>(LogLevel.Information, "Category", 0, state, e, Formatter);

        amend?.Invoke(logEntry);

        return logEntry;
    }
}
#endif
