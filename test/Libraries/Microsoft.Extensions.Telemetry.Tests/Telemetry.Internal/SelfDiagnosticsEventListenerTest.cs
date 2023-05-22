// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.Tracing;
using System.Globalization;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Text;
using Microsoft.Extensions.Time.Testing;
using Moq;
using Xunit;

namespace Microsoft.Extensions.Telemetry.Internal.Test;

/// <summary>
/// Copied from https://github.com/open-telemetry/opentelemetry-dotnet/blob/952c3b17fc2eaa0622f5f3efd336d4cf103c2813/test/OpenTelemetry.Tests/Internal/SelfDiagnosticsEventListenerTest.cs.
/// </summary>
public class SelfDiagnosticsEventListenerTest
{
    private const string LogFilePath = "Diagnostics.log";
    private const string Ellipses = "...\n";
    private const string EllipsesWithBrackets = "{...}\n";

    private static readonly FakeTimeProvider _timeProvider = new();

    [Fact(Skip = "Flaky")]
    public void SelfDiagnosticsEventListener_constructor_Invalid_Input()
    {
        // no configRefresher object
        Assert.Throws<ArgumentNullException>(() =>
        {
            _ = new SelfDiagnosticsEventListener(EventLevel.Error, null!, _timeProvider);
        });
    }

    [Fact(Skip = "Flaky")]
    public void SelfDiagnosticsEventListener_CanDispose()
    {
        var configRefresherMock = new Mock<SelfDiagnosticsConfigRefresher>(_timeProvider, null, string.Empty, null);
        var listener = new SelfDiagnosticsEventListener(EventLevel.Error, configRefresherMock.Object, _timeProvider);

        var exc1 = Record.Exception(() => listener.Dispose());
        var exc2 = Record.Exception(() => listener.Dispose());

        Assert.Null(exc1);
        Assert.Null(exc2);

        listener.Dispose();
    }

    [Fact(Skip = "Flaky")]
    public void SelfDiagnosticsEventListener_EventSourceSetup_LowerSeverity()
    {
        var configRefresherMock = new Mock<SelfDiagnosticsConfigRefresher>(_timeProvider, null, string.Empty, null);
        using var listener = new SelfDiagnosticsEventListener(EventLevel.Error, configRefresherMock.Object, _timeProvider);

        // Emitting a Warning event. Or any EventSource event with lower severity than Error.
        TestEventSource.Log.WarningEvent();
        configRefresherMock.Verify(refresher => refresher.TryGetLogStream(It.IsAny<int>(), out It.Ref<Stream>.IsAny, out It.Ref<int>.IsAny), Times.Never());
    }

    [Fact(Skip = "Flaky")]
    public void SelfDiagnosticsEventListener_EventSourceSetup_HigherSeverity()
    {
        var configRefresherMock = new Mock<SelfDiagnosticsConfigRefresher>(_timeProvider, null, string.Empty, null);
        configRefresherMock
            .Setup(configRefresher => configRefresher.TryGetLogStream(It.IsAny<int>(), out It.Ref<Stream>.IsAny, out It.Ref<int>.IsAny))
            .Returns(true);
        using var listener = new SelfDiagnosticsEventListener(EventLevel.Error, configRefresherMock.Object, _timeProvider);

        // Emitting an Error event. Or any EventSource event with higher than or equal to to Error severity.
        TestEventSource.Log.ErrorEvent();
        configRefresherMock.Verify(refresher => refresher.TryGetLogStream(It.IsAny<int>(), out It.Ref<Stream>.IsAny, out It.Ref<int>.IsAny));
    }

    [Fact(Skip = "Flaky")]
    public void SelfDiagnosticsEventListener_WriteEvent()
    {
        // Arrange
        var payload = new ReadOnlyCollection<object?>(
            new List<object?>
            {
                new(),
                "test payload item",
                null
            });
        const string PayloadToString = "{System.Object}{test payload item}{null}";

        var configRefresherMock = new Mock<SelfDiagnosticsConfigRefresher>(_timeProvider, null, string.Empty, null);
        var memoryMappedFile = MemoryMappedFile.CreateFromFile(LogFilePath, FileMode.Create, null, 1024);
        Stream stream = memoryMappedFile.CreateViewStream();
        string eventMessage = "Event Message";
        int timestampPrefixLength = "2020-08-14T20:33:24.4788109Z:".Length;
        byte[] bytes = Encoding.UTF8.GetBytes(eventMessage + PayloadToString);
        int availableByteCount = 140;
        configRefresherMock
            .Setup(configRefresher => configRefresher.TryGetLogStream(timestampPrefixLength + bytes.Length + 1, out stream, out availableByteCount))
            .Returns(true);
        using var listener = new SelfDiagnosticsEventListener(EventLevel.Error, configRefresherMock.Object, _timeProvider);

        // Act: call WriteEvent method directly
        listener.WriteEvent(eventMessage, payload);

        // Assert
        configRefresherMock.Verify(refresher => refresher.TryGetLogStream(timestampPrefixLength + bytes.Length + 1, out stream, out availableByteCount));
        stream.Dispose();
        memoryMappedFile.Dispose();
        AssertFileOutput(LogFilePath, eventMessage + PayloadToString);

        File.Delete(LogFilePath);
    }

    [Fact(Skip = "Flaky")]
    public void SelfDiagnosticsEventListener_DateTimeGetBytes()
    {
        var configRefresherMock = new Mock<SelfDiagnosticsConfigRefresher>(_timeProvider, null, string.Empty, null);
        using var listener = new SelfDiagnosticsEventListener(EventLevel.Error, configRefresherMock.Object, _timeProvider);

        // Check DateTimeKind of Utc, Local, and Unspecified
        DateTime[] datetimes =
        {
            DateTime.SpecifyKind(DateTime.Parse("1996-12-01T14:02:31.1234567-08:00", CultureInfo.InvariantCulture), DateTimeKind.Utc),
            DateTime.SpecifyKind(DateTime.Parse("1996-12-01T14:02:31.1234567-08:00", CultureInfo.InvariantCulture), DateTimeKind.Local),
            DateTime.SpecifyKind(DateTime.Parse("1996-12-01T14:02:31.1234567-08:00", CultureInfo.InvariantCulture), DateTimeKind.Unspecified),
            DateTime.UtcNow,
            DateTime.Now,
        };

        // Expect to match output string from DateTime.ToString("O")
        string[] expected = new string[datetimes.Length];
        for (int i = 0; i < datetimes.Length; i++)
        {
            expected[i] = datetimes[i].ToString("O", CultureInfo.InvariantCulture);
        }

        byte[] buffer = new byte[40 * datetimes.Length];
        int pos = 0;

        // Get string after DateTimeGetBytes() write into a buffer
        string[] results = new string[datetimes.Length];
        for (int i = 0; i < datetimes.Length; i++)
        {
            int len = listener.DateTimeGetBytes(datetimes[i], buffer, pos);
            results[i] = Encoding.Default.GetString(buffer, pos, len);
            pos += len;
        }

        Assert.Equal(expected, results);
    }

    [InlineData(5, '+')]
    [InlineData(-3, '-')]
    [InlineData(0, '+')]
    [Theory(Skip = "Flaky")]
    public void TestSign(int hours, char expected)
    {
        Assert.Equal((byte)expected, SelfDiagnosticsEventListener.GetHoursSign(hours));
    }

    [Fact(Skip = "Flaky")]
    public void SelfDiagnosticsEventListener_EmitEvent_OmitAsConfigured()
    {
        // Arrange
        var configRefresherMock = new Mock<SelfDiagnosticsConfigRefresher>(_timeProvider, null, string.Empty, null);
        var memoryMappedFile = MemoryMappedFile.CreateFromFile(LogFilePath, FileMode.Create, null, 1024);
        Stream stream = memoryMappedFile.CreateViewStream();
        configRefresherMock
            .Setup(configRefresher => configRefresher.TryGetLogStream(It.IsAny<int>(), out stream, out It.Ref<int>.IsAny))
            .Returns(true);
        using var listener = new SelfDiagnosticsEventListener(EventLevel.Error, configRefresherMock.Object, _timeProvider);

        // Act: emit an event with severity lower than configured
        TestEventSource.Log.WarningEvent();

        // Assert
        configRefresherMock.Verify(refresher => refresher.TryGetLogStream(It.IsAny<int>(), out stream, out It.Ref<int>.IsAny), Times.Never());
        stream.Dispose();
        memoryMappedFile.Dispose();

        using var file = File.Open(LogFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        var buffer = new byte[256];
        _ = file.Read(buffer, 0, buffer.Length);
        Assert.Equal('\0', (char)buffer[0]);
    }

    [Fact(Skip = "Flaky")]
    public void SelfDiagnosticsEventListener_EmitEvent_CaptureAsConfigured()
    {
        // Arrange
        var configRefresherMock = new Mock<SelfDiagnosticsConfigRefresher>(_timeProvider, null, string.Empty, null);
        var memoryMappedFile = MemoryMappedFile.CreateFromFile(LogFilePath, FileMode.Create, null, 1024);
        Stream stream = memoryMappedFile.CreateViewStream();
        configRefresherMock
            .Setup(configRefresher => configRefresher.TryGetLogStream(It.IsAny<int>(), out stream, out It.Ref<int>.IsAny))
            .Returns(true);
        using var listener = new SelfDiagnosticsEventListener(EventLevel.Error, configRefresherMock.Object, _timeProvider);

        // Act: emit an event with severity equal to configured
        TestEventSource.Log.ErrorEvent();

        // Assert
        configRefresherMock.Verify(refresher => refresher.TryGetLogStream(It.IsAny<int>(), out stream, out It.Ref<int>.IsAny));
        stream.Dispose();
        memoryMappedFile.Dispose();

        AssertFileOutput(LogFilePath, TestEventSource.ErrorMessageText);
    }

    [Fact(Skip = "Flaky")]
    public void SelfDiagnosticsEventListener_EncodeInBuffer_Null()
    {
        byte[] buffer = new byte[20];
        int startPos = 0;
        int endPos = SelfDiagnosticsEventListener.EncodeInBuffer(null, false, buffer, startPos);
        Assert.Equal(startPos, endPos);
    }

    [Fact(Skip = "Flaky")]
    public void SelfDiagnosticsEventListener_EncodeInBuffer_Empty()
    {
        byte[] buffer = new byte[20];
        int startPos = 0;
        int endPos = SelfDiagnosticsEventListener.EncodeInBuffer(string.Empty, false, buffer, startPos);
        byte[] expected = Encoding.UTF8.GetBytes(string.Empty);
        AssertBufferOutput(expected, buffer, startPos, endPos);
    }

    [Fact(Skip = "Flaky")]
    public void SelfDiagnosticsEventListener_EncodeInBuffer_EnoughSpace()
    {
        byte[] buffer = new byte[20];
        int startPos = buffer.Length - Ellipses.Length - 6;  // Just enough space for "abc" even if "...\n" needs to be added.
        int endPos = SelfDiagnosticsEventListener.EncodeInBuffer("abc", false, buffer, startPos);

        // '\n' will be appended to the original string "abc" after EncodeInBuffer is called.
        // The byte where '\n' will be placed should not be touched within EncodeInBuffer, so it stays as '\0'.
        byte[] expected = Encoding.UTF8.GetBytes("abc\0");
        AssertBufferOutput(expected, buffer, startPos, endPos + 1);
    }

    [Fact(Skip = "Flaky")]
    public void SelfDiagnosticsEventListener_EncodeInBuffer_NotEnoughSpaceForFullString()
    {
        byte[] buffer = new byte[20];
        int startPos = buffer.Length - Ellipses.Length - 5;  // Just not space for "abc" if "...\n" needs to be added.

        // It's a quick estimate by assumption that most Unicode characters takes up to 2 16-bit UTF-16 chars,
        // which can be up to 4 bytes when encoded in UTF-8.
        int endPos = SelfDiagnosticsEventListener.EncodeInBuffer("abc", false, buffer, startPos);
        byte[] expected = Encoding.UTF8.GetBytes("ab...\0");
        AssertBufferOutput(expected, buffer, startPos, endPos + 1);
    }

    [Fact(Skip = "Flaky")]
    public void SelfDiagnosticsEventListener_EncodeInBuffer_NotEvenSpaceForTruncatedString()
    {
        byte[] buffer = new byte[20];
        int startPos = buffer.Length - Ellipses.Length;  // Just enough space for "...\n".
        int endPos = SelfDiagnosticsEventListener.EncodeInBuffer("abc", false, buffer, startPos);
        byte[] expected = Encoding.UTF8.GetBytes("...\0");
        AssertBufferOutput(expected, buffer, startPos, endPos + 1);
    }

    [Fact(Skip = "Flaky")]
    public void SelfDiagnosticsEventListener_EncodeInBuffer_NotEvenSpaceForTruncationEllipses()
    {
        byte[] buffer = new byte[20];
        int startPos = buffer.Length - Ellipses.Length + 1;  // Not enough space for "...\n".
        int endPos = SelfDiagnosticsEventListener.EncodeInBuffer("abc", false, buffer, startPos);
        Assert.Equal(startPos, endPos);
    }

    [Fact(Skip = "Flaky")]
    public void SelfDiagnosticsEventListener_EncodeInBuffer_IsParameter_EnoughSpace()
    {
        byte[] buffer = new byte[20];
        int startPos = buffer.Length - EllipsesWithBrackets.Length - 6;  // Just enough space for "abc" even if "...\n" need to be added.
        int endPos = SelfDiagnosticsEventListener.EncodeInBuffer("abc", true, buffer, startPos);
        byte[] expected = Encoding.UTF8.GetBytes("{abc}\0");
        AssertBufferOutput(expected, buffer, startPos, endPos + 1);
    }

    [Fact(Skip = "Flaky")]
    public void SelfDiagnosticsEventListener_EncodeInBuffer_IsParameter_NotEnoughSpaceForFullString()
    {
        byte[] buffer = new byte[20];
        int startPos = buffer.Length - EllipsesWithBrackets.Length - 5;  // Just not space for "...\n".
        int endPos = SelfDiagnosticsEventListener.EncodeInBuffer("abc", true, buffer, startPos);
        byte[] expected = Encoding.UTF8.GetBytes("{ab...}\0");
        AssertBufferOutput(expected, buffer, startPos, endPos + 1);
    }

    [Fact(Skip = "Flaky")]
    public void SelfDiagnosticsEventListener_EncodeInBuffer_IsParameter_NotEvenSpaceForTruncatedString()
    {
        byte[] buffer = new byte[20];
        int startPos = buffer.Length - EllipsesWithBrackets.Length;  // Just enough space for "{...}\n".
        int endPos = SelfDiagnosticsEventListener.EncodeInBuffer("abc", true, buffer, startPos);
        byte[] expected = Encoding.UTF8.GetBytes("{...}\0");
        AssertBufferOutput(expected, buffer, startPos, endPos + 1);
    }

    [Fact(Skip = "Flaky")]
    public void SelfDiagnosticsEventListener_EncodeInBuffer_IsParameter_NotEvenSpaceForTruncationEllipses()
    {
        byte[] buffer = new byte[20];
        int startPos = buffer.Length - EllipsesWithBrackets.Length + 1;  // Not enough space for "{...}\n".
        int endPos = SelfDiagnosticsEventListener.EncodeInBuffer("abc", true, buffer, startPos);
        Assert.Equal(startPos, endPos);
    }

    private static void AssertFileOutput(string filePath, string eventMessage)
    {
        using var file = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        var buffer = new byte[256];
        _ = file.Read(buffer, 0, buffer.Length);
        string logLine = Encoding.UTF8.GetString(buffer);
        string logMessage = ParseLogMessage(logLine);
        Assert.StartsWith(eventMessage, logMessage);
    }

    private static string ParseLogMessage(string logLine)
    {
        int timestampPrefixLength = "2020-08-14T20:33:24.4788109Z:".Length;
        Assert.Matches(@"\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}\.\d{7}Z:", logLine.Substring(0, timestampPrefixLength));
        return logLine.Substring(timestampPrefixLength);
    }

    private static void AssertBufferOutput(byte[] expected, byte[] buffer, int startPos, int endPos)
    {
        Assert.Equal(expected.Length, endPos - startPos);
        for (int i = 0, j = startPos; j < endPos; ++i, ++j)
        {
            Assert.Equal(expected[i], buffer[j]);
        }
    }
}
