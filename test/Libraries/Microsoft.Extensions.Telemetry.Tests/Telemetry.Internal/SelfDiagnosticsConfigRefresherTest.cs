// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.Tracing;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using Microsoft.Extensions.Time.Testing;
using Microsoft.TestUtilities;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Extensions.Telemetry.Internal.Test;

/// <summary>
/// Copied from https://github.com/open-telemetry/opentelemetry-dotnet/blob/952c3b17fc2eaa0622f5f3efd336d4cf103c2813/test/OpenTelemetry.Tests/Internal/SelfDiagnosticsConfigRefresherTest.cs.
/// </summary>
public class SelfDiagnosticsConfigRefresherTest
{
    private const int BufferSize = 512;

    private static readonly byte[] _messageOnNewFile = SelfDiagnosticsConfigRefresher.MessageOnNewFile;
    private static readonly string _messageOnNewFileString = Encoding.UTF8.GetString(SelfDiagnosticsConfigRefresher.MessageOnNewFile);
    private static readonly FakeTimeProvider _timeProvider = new();

    private readonly Mock<SelfDiagnosticsConfigParser> _configParserMock = new();
    private readonly ITestOutputHelper _output;

#pragma warning disable IDE0052 // Remove unread private members - read into the mock's out param.
    private string _configFileContent = @"{""LogDirectory"": ""."", ""FileSize"": 1024, ""LogLevel"": ""Error""}";
#pragma warning restore IDE0052 // Remove unread private members

    public SelfDiagnosticsConfigRefresherTest(ITestOutputHelper output)
    {
        _configParserMock.Setup(c => c.TryReadConfigFile(It.IsAny<string>(), out _configFileContent)).Returns(true);
        _configParserMock.Setup(c => c.SetFileSizeWithinLimit(1024)).Returns(1024);

        _output = output;
    }

    [Fact(Skip = "Flaky")]
    public void SelfDiagnosticsConfigRefresher_OmitAsConfigured()
    {
        const string LogFileName = "omit.log";

        try
        {
            using SelfDiagnosticsConfigRefresher configRefresher = new SelfDiagnosticsConfigRefresher(_timeProvider, _configParserMock.Object, LogFileName);

            // Emitting event of EventLevel.Warning
            TestEventSource.Log.WarningEvent();

            var actualBytes = ReadFile(BufferSize, LogFileName);
            string logText = Encoding.UTF8.GetString(actualBytes);
            _output.WriteLine(logText);  // for debugging in case the test fails
            Assert.StartsWith(_messageOnNewFileString, logText);

            // The event was omitted
            Assert.Equal('\0', (char)actualBytes[_messageOnNewFile.Length]);
        }
        finally
        {
            RemoveLogFile(LogFileName);
        }
    }

    [Fact(Skip = "Flaky")]
    public void SelfDiagnosticsConfigRefresher_WhenConfigIsAvailable_CaptureAsConfigured()
    {
        const string LogFileName = "capture.log";
        const int MessageLength = 941;
        var fixture = new Fixture();
        var configFileContent = @"{""LogDirectory"": ""."", ""FileSize"": 1, ""LogLevel"": ""Error""}";
        var configParserMock = new Mock<SelfDiagnosticsConfigParser>();
        configParserMock.Setup(c => c.TryReadConfigFile(It.IsAny<string>(), out configFileContent)).Returns(true);
        configParserMock.Setup(c => c.SetFileSizeWithinLimit(It.IsAny<int>())).Returns<int>(x => x);

        // or any string longer than configured default 1024kb to test file overflow.
        var longString = string.Join(string.Empty, fixture.CreateMany<char>(MessageLength));
        try
        {
            using SelfDiagnosticsConfigRefresher configRefresher = new(_timeProvider, configParserMock.Object, LogFileName);

            // Emitting event of EventLevel.Critical or any level equal or higher than the configured EventLevel.Error
            TestEventSource.Log.CriticalEvent(longString);

            byte[] actualBytes = ReadFile(MessageLength + BufferSize, LogFileName);
            string logText = Encoding.UTF8.GetString(actualBytes);
            Assert.StartsWith(_messageOnNewFileString, logText);

            // The event was captured
            string logLine = logText.Substring(_messageOnNewFileString.Length);
            string logMessage = ParseLogMessage(logLine);

            Assert.StartsWith(TestEventSource.CriticalMessageText, logMessage);
        }
        finally
        {
            RemoveLogFile(LogFileName);
        }
    }

    [Fact(Skip = "Flaky")]
    public void TryGetLogStream_WhenViewStreamDisposed_ReturnsFalse()
    {
        const string LogFileName = "noViewStream.log";
        Stream? stream = null;
        var timeProvider = new FakeTimeProvider();

        try
        {
            using SelfDiagnosticsConfigRefresher configRefresher = new(timeProvider, _configParserMock.Object, LogFileName);
            configRefresher.ViewStream.Dispose();
            var result = configRefresher.TryGetLogStream(100, out stream, out var availableByteCount);

            Assert.False(result);
            Assert.Equal(Stream.Null, stream);
            Assert.Equal(0, availableByteCount);
        }
        catch (ObjectDisposedException)
        {
            // After disposing the ThreadLocal ViewStream, configRefresher is being disposed too and throws, here we are catching it.
        }
        finally
        {
            stream?.Dispose();
            File.Delete(LogFileName);
        }
    }

    [Fact(Skip = "Flaky")]
    public async Task SelfDiagnosticsConfigRefresher_WhenConfigDisappearsAndAppearsBack_CaptureAsConfigured()
    {
        const string LogFileName = "withUnreliableConfig.log";
        var timeProvider = new FakeTimeProvider(DateTime.UtcNow);
        var parserMock = new Mock<SelfDiagnosticsConfigParser>();
        var configFileContentInitial = @"{""LogDirectory"": ""."", ""FileSize"": 1024, ""LogLevel"": ""Verbose""}";
        var configFileContentNew = @"{""LogDirectory"": ""."", ""FileSize"": 1025, ""LogLevel"": ""Verbose""}";
        parserMock.Setup(parser => parser.TryReadConfigFile(It.IsAny<string>(), out configFileContentInitial)).Returns(true);
        parserMock.Setup(parser => parser.SetFileSizeWithinLimit(It.IsAny<int>())).CallBase();

        try
        {
            using SelfDiagnosticsConfigRefresher configRefresher = new(timeProvider, parserMock.Object, LogFileName);
            timeProvider.Advance(TimeSpan.FromSeconds(10)); // give the SelfDiagnosticsConfigRefresher's ctor to pick up the initial config.
            parserMock.Setup(parser => parser.TryReadConfigFile(It.IsAny<string>(), out configFileContentInitial)).Returns(false); // pretending that config file was removed.
            TestEventSource.Log.VerboseEvent(); // that event will be dropped
            parserMock.Setup(parser => parser.TryReadConfigFile(It.IsAny<string>(), out configFileContentNew)).Returns(true); // restoring config back.
            timeProvider.Advance(TimeSpan.FromSeconds(10)); // give the SelfDiagnosticsConfigRefresher's worker thread time to pick up the new config.
            await Task.Delay(TimeSpan.FromSeconds(1));
            TestEventSource.Log.VerboseEvent();
            byte[] actualBytes = ReadFile(BufferSize, LogFileName);
            string logText = Encoding.UTF8.GetString(actualBytes);
            Assert.StartsWith(_messageOnNewFileString, logText);

            // The event was captured
            string logLine = logText.Substring(_messageOnNewFileString.Length);
            string logMessage = ParseLogMessage(logLine);

            Assert.StartsWith(TestEventSource.VerboseMessageText, logMessage);
        }
        finally
        {
            RemoveLogFile(LogFileName);
        }
    }

    [Fact(Skip = "Flaky")]
    public async Task SelfDiagnosticsConfigRefresher_WhenLogLevelUpdated_CaptureAsConfigured()
    {
        const string LogFileName = "withNewLogLevel.log";
        var timeProvider = new FakeTimeProvider(DateTime.UtcNow);
        var parserMock = new Mock<SelfDiagnosticsConfigParser>();
        var configFileContentInitial = @"{""LogDirectory"": ""."", ""FileSize"": 1024, ""LogLevel"": ""Error""}";
        var configFileContentNew = @"{""LogDirectory"": ""."", ""FileSize"": 1024, ""LogLevel"": ""Verbose""}";
        parserMock.Setup(parser => parser.TryReadConfigFile(It.IsAny<string>(), out configFileContentInitial)).Returns(true);
        parserMock.Setup(parser => parser.SetFileSizeWithinLimit(It.IsAny<int>())).CallBase();

        try
        {
            using SelfDiagnosticsConfigRefresher configRefresher = new(timeProvider, parserMock.Object, LogFileName);
            timeProvider.Advance(TimeSpan.FromSeconds(10)); // give the SelfDiagnosticsConfigRefresher's ctor to pick up the initial config.
            TestEventSource.Log.ErrorEvent();
            await Task.Delay(TimeSpan.FromSeconds(10));
            parserMock.Setup(parser => parser.TryReadConfigFile(It.IsAny<string>(), out configFileContentNew)).Returns(true); // updating log level.
            timeProvider.Advance(TimeSpan.FromSeconds(10)); // give the SelfDiagnosticsConfigRefresher's worker thread time to pick up the new config.
            await Task.Delay(TimeSpan.FromSeconds(5));
            TestEventSource.Log.VerboseEvent();

            var outputFilePath = Path.Combine(".", LogFileName);
            var times = 3;
            string logText = string.Empty;
            string logMessage = string.Empty;

            // checking until the file has the right content,
            // because the file is updated in a different thread,
            // which we have no access to.
            while (times > 0)
            {
                using var file = File.Open(outputFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                byte[] actualBytes = new byte[BufferSize];
                _ = file.Read(actualBytes, 0, BufferSize);
                logText = Encoding.UTF8.GetString(actualBytes);
                var logLine = logText.Substring(_messageOnNewFileString.Length);
                logMessage = ParseLogMessage(logLine);

                if (logMessage.Contains(TestEventSource.VerboseMessageText))
                {
                    break;
                }

                await timeProvider.Delay(
                    TimeSpan.FromSeconds(10),
                    CancellationToken.None)
                    .ConfigureAwait(false);
                times--;
            }

            Assert.StartsWith(_messageOnNewFileString, logText);
            Assert.Contains(TestEventSource.VerboseMessageText, logMessage);
            Assert.StartsWith(TestEventSource.ErrorMessageText, logMessage);
        }
        finally
        {
            RemoveLogFile(LogFileName);
        }
    }

    [Fact(Skip = "Flaky")]
    public async Task SelfDiagnosticsConfigRefresher_WhenOneStreamDisposed_WorksCorrectly()
    {
        // Arrange
        const string LogFileName = "withDisposedStream.log";
        var timeProvider = TimeProvider.System;
        var parserMock = new Mock<SelfDiagnosticsConfigParser>();
        var configFileContentInitial = @"{""LogDirectory"": ""."", ""FileSize"": 1024, ""LogLevel"": ""Verbose""}";
        parserMock.Setup(parser => parser.TryReadConfigFile(It.IsAny<string>(), out configFileContentInitial)).Returns(true);
        parserMock.Setup(parser => parser.SetFileSizeWithinLimit(It.IsAny<int>())).CallBase();

        using var configRefresher = new SelfDiagnosticsConfigRefresher(timeProvider, parserMock.Object, LogFileName);
        using var listener = new SelfDiagnosticsEventListener(EventLevel.Error, configRefresher, timeProvider);

        await timeProvider.Delay(TimeSpan.FromSeconds(10), CancellationToken.None); // give the SelfDiagnosticsConfigRefresher's ctor time to pick up the initial config.
        _ = configRefresher.TryGetLogStream(100, out _, out _); // opening the file, i.e. creating a stream

        Stream? stream = null;
        try
        {
            // Act
            configRefresher.ViewStream.Value = null;
            var exception = Record.Exception(() => configRefresher.Dispose());
            var result = configRefresher.TryGetLogStream(100, out stream, out var availableByteCount);

            // Assert
            Assert.Null(exception);
            Assert.False(result);
            Assert.Equal(Stream.Null, stream);
            Assert.Equal(0, availableByteCount);
        }
        finally
        {
            stream?.Dispose();
            RemoveLogFile(LogFileName);
        }
    }

    [Fact(Skip = "Flaky")]
    public async Task WorkerAsync_WhenTaskCancelled_CorrectlyStops()
    {
        // Arrange
        var timeProvider = System.TimeProvider.System;
        var parserMock = new Mock<SelfDiagnosticsConfigParser>();
        parserMock.Setup(parser => parser.TryReadConfigFile(It.IsAny<string>(), out It.Ref<string>.IsAny)).Returns(false);
        parserMock.Setup(parser => parser.SetFileSizeWithinLimit(It.IsAny<int>())).CallBase();

        using var configRefresher = new SelfDiagnosticsConfigRefresher(timeProvider, parserMock.Object);

        using var cts = new CancellationTokenSource();
        cts.CancelAfter(10000);
        try
        {
            await configRefresher.WorkerAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            // no.
        }

        parserMock.Verify(p => p.TryReadConfigFile(It.IsAny<string>(), out It.Ref<string>.IsAny), Times.AtMost(3));
    }

    [Fact(Skip = "Flaky")]
    public void Worker_WhenClockDoesNotThrow_CorrectlyStops()
    {
        // Arrange
        var timeProvider = new FakeTimeProvider();
        var parserMock = Mock.Of<SelfDiagnosticsConfigParser>();
        Assert.Null(Record.Exception(() =>
        {
            var configRefresher = new SelfDiagnosticsConfigRefresher(timeProvider, parserMock, workerTaskToken: CancellationToken.None);
            configRefresher.Dispose();
        }));
    }

    [Fact(Skip = "Flaky")]
    public void SelfDiagnosticsConfigRefresher_WhenNoConfigFile_CannotGetLogStream()
    {
        const string LogFileName = "noFile.log";
        Stream? stream = null;

        try
        {
            var configRefresher = new SelfDiagnosticsConfigRefresher(_timeProvider, _configParserMock.Object, LogFileName);
            configRefresher.Dispose();

            var result = configRefresher.TryGetLogStream(1, out stream, out var availableByteCount);

            Assert.False(result);
            Assert.Equal(Stream.Null, stream);
            Assert.Equal(0, availableByteCount);
        }
        finally
        {
            stream?.Dispose();
            RemoveLogFile(LogFileName);
        }
    }

    [Fact(Skip = "Flaky")]
    public void SelfDiagnosticsConfigRefresher_InvalidDirectory_WritesEventSourceEvent()
    {
        using var listener = new TestEventListener(SelfDiagnosticsEventSource.Log, EventLevel.Warning);

        var invalidDirectory = AppContext.BaseDirectory + "\\nul";
        var configParserMock = new Mock<SelfDiagnosticsConfigParser>();
        var configFileContent = $"{{\"LogDirectory\": \"{invalidDirectory}\", \"FileSize\": 1024, \"LogLevel\": \"Error\"}}"; // nul is a Windows reserved name.
        configParserMock.Setup(c => c.TryReadConfigFile(It.IsAny<string>(), out configFileContent)).Returns(true);
        using var configRefresher = new SelfDiagnosticsConfigRefresher(_timeProvider, configParserMock.Object);

        var lastEvent = listener.LastEvent;

        Assert.NotNull(lastEvent);
        Assert.Equal(SelfDiagnosticsEventSource.FileCreateExceptionEventId, lastEvent!.EventId);
        Assert.Equal(EventLevel.Warning, lastEvent!.Level);
        Assert.Contains(invalidDirectory, lastEvent!.Payload!);
    }

    [ConditionalFact]
    [OSSkipCondition(OperatingSystems.Linux, SkipReason = "See https://github.com/dotnet/r9/issues/97")]
    public void TryGetLogStream_MemoryMappedFileCache_NotEqual_MemoryMappedFile_ReturnsTrue()
    {
        const string LogFileName = "noMemoryMappedFileCache.log";
        Stream? stream = null;
        var timeProvider = new FakeTimeProvider();

        try
        {
            using SelfDiagnosticsConfigRefresher configRefresher = new(timeProvider, _configParserMock.Object, LogFileName);
            using MemoryMappedFile testFile = MemoryMappedFile.CreateNew("TestName", 100);
            using MemoryMappedViewStream testViewStream = testFile.CreateViewStream();
            configRefresher.ViewStream.Value = testViewStream;
            Assert.NotNull(testViewStream);
            Assert.NotNull(configRefresher.ViewStream.Value);
            configRefresher.MemoryMappedFileCache.Value = null!;
            var result = configRefresher.TryGetLogStream(100, out stream, out var availableByteCount);

            Assert.True(result);
        }
        catch (ObjectDisposedException)
        {
            // After disposing the ThreadLocal ViewStream, configRefresher is being disposed too and throws, here we are catching it.
        }
        finally
        {
            stream?.Dispose();
            File.Delete(LogFileName);
        }
    }

    private static string ParseLogMessage(string logLine)
    {
        int timestampPrefixLength = "2020-08-14T20:33:24.4788109Z:".Length;
        Assert.Matches(@"\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}\.\d{7}Z:", logLine.Substring(0, timestampPrefixLength));
        return logLine.Substring(timestampPrefixLength);
    }

    private static byte[] ReadFile(int byteCount, string logFileName)
    {
        var outputFilePath = Path.Combine(".", logFileName);
        using var file = File.Open(outputFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        byte[] actualBytes = new byte[byteCount];
        _ = file.Read(actualBytes, 0, byteCount);
        return actualBytes;
    }

    private static void RemoveLogFile(string filePath)
    {
        try
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
#pragma warning disable CA1031 // Do not catch general exception types
        catch (Exception)
        {
            // no handling.
        }
#pragma warning restore CA1031 // Do not catch general exception types

    }
}
