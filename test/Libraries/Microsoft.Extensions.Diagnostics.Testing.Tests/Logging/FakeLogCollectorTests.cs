// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Time.Testing;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Extensions.Logging.Testing.Test.Logging;

public class FakeLogCollectorTests
{
    private class Output : ITestOutputHelper
    {
        public string Last { get; private set; } = string.Empty;

        public void WriteLine(string message)
        {
            Last = message;
        }

        public void WriteLine(string format, params object[] args) => WriteLine(string.Format(CultureInfo.InvariantCulture, format, args));
    }

    [Fact]
    public void Basic()
    {
        var output = new Output();

        var timeProvider = new FakeTimeProvider();
        timeProvider.Advance(TimeSpan.FromMilliseconds(1));

        var options = new FakeLogCollectorOptions
        {
            OutputSink = output.WriteLine,
            TimeProvider = timeProvider,
        };

        var collector = FakeLogCollector.Create(options);
        var logger = new FakeLogger(collector);

        logger.LogTrace("Hello world!");
        Assert.Equal("[00:00.001, trace] Hello world!", output.Last);

        logger.LogDebug("Hello world!");
        Assert.Equal("[00:00.001, debug] Hello world!", output.Last);

        logger.LogInformation("Hello world!");
        Assert.Equal("[00:00.001,  info] Hello world!", output.Last);

        logger.LogWarning("Hello world!");
        Assert.Equal("[00:00.001,  warn] Hello world!", output.Last);

        logger.LogError("Hello world!");
        Assert.Equal("[00:00.001, error] Hello world!", output.Last);

        logger.LogCritical("Hello world!");
        Assert.Equal("[00:00.001,  crit] Hello world!", output.Last);

        logger.Log(LogLevel.None, "Hello world!");
        Assert.Equal("[00:00.001,  none] Hello world!", output.Last);

        logger.Log((LogLevel)42, "Hello world!");
        Assert.Equal("[00:00.001, invld] Hello world!", output.Last);
    }

    [Fact]
    public void DIEntryPoint()
    {
        var output = new Output();

        var timeProvider = new FakeTimeProvider();
        timeProvider.Advance(TimeSpan.FromMilliseconds(1));

        var options = new FakeLogCollectorOptions
        {
            OutputSink = output.WriteLine,
            TimeProvider = timeProvider,
        };

        var collector = new FakeLogCollector(Microsoft.Extensions.Options.Options.Create(options));
        var logger = new FakeLogger(collector);

        logger.LogTrace("Hello world!");
        Assert.Equal("[00:00.001, trace] Hello world!", output.Last);

        logger.LogDebug("Hello world!");
        Assert.Equal("[00:00.001, debug] Hello world!", output.Last);

        logger.LogInformation("Hello world!");
        Assert.Equal("[00:00.001,  info] Hello world!", output.Last);

        logger.LogWarning("Hello world!");
        Assert.Equal("[00:00.001,  warn] Hello world!", output.Last);

        logger.LogError("Hello world!");
        Assert.Equal("[00:00.001, error] Hello world!", output.Last);

        logger.LogCritical("Hello world!");
        Assert.Equal("[00:00.001,  crit] Hello world!", output.Last);

        logger.Log(LogLevel.None, "Hello world!");
        Assert.Equal("[00:00.001,  none] Hello world!", output.Last);

        logger.Log((LogLevel)42, "Hello world!");
        Assert.Equal("[00:00.001, invld] Hello world!", output.Last);
    }

    [Fact]
    public void DIEntryPoint_NullChecks()
    {
        Assert.Throws<ArgumentNullException>(() => new FakeLogCollector(null!));
        Assert.Throws<ArgumentException>(() => new FakeLogCollector(Microsoft.Extensions.Options.Options.Create((FakeLogCollectorOptions)null!)));
    }

    [Fact]
    public void TestOutputHelperExtensionsNonGeneric()
    {
        var output = new Output();

        var logger = new FakeLogger(output.WriteLine, "Storage");

        logger.LogTrace("Hello world!");
        Assert.Contains("trace] Hello world!", output.Last);

        logger.LogDebug("Hello world!");
        Assert.Contains("debug] Hello world!", output.Last);

        logger.LogInformation("Hello world!");
        Assert.Contains("info] Hello world!", output.Last);

        logger.LogWarning("Hello world!");
        Assert.Contains("warn] Hello world!", output.Last);

        logger.LogError("Hello world!");
        Assert.Contains("error] Hello world!", output.Last);

        logger.LogCritical("Hello world!");
        Assert.Contains("crit] Hello world!", output.Last);

        logger.Log(LogLevel.None, "Hello world!");
        Assert.Contains("none] Hello world!", output.Last);

        logger.Log((LogLevel)42, "Hello world!");
        Assert.Contains("invld] Hello world!", output.Last);
    }

    [Fact]
    public void TestOutputHelperExtensionsGeneric()
    {
        var output = new Output();

        var logger = new FakeLogger<FakeLogCollectorTests>(output.WriteLine);

        logger.LogTrace("Hello world!");
        Assert.Contains("trace] Hello world!", output.Last);

        logger.LogDebug("Hello world!");
        Assert.Contains("debug] Hello world!", output.Last);

        logger.LogInformation("Hello world!");
        Assert.Contains("info] Hello world!", output.Last);

        logger.LogWarning("Hello world!");
        Assert.Contains("warn] Hello world!", output.Last);

        logger.LogError("Hello world!");
        Assert.Contains("error] Hello world!", output.Last);

        logger.LogCritical("Hello world!");
        Assert.Contains("crit] Hello world!", output.Last);

        logger.Log(LogLevel.None, "Hello world!");
        Assert.Contains("none] Hello world!", output.Last);

        logger.Log((LogLevel)42, "Hello world!");
        Assert.Contains("invld] Hello world!", output.Last);
    }

    private record WaitingTestCase(
        int EndWaitAtAttemptCount,
        int? CancellationWaitInMs,
        int? TimeoutWaitInMs,
        bool StartsWithCancelledToken,
        string[] ExpectedOperationSequence,
        bool ExpectedTaskCancelled,
        bool? ExpectedAwaitedTaskResult
    );

    private const int WaitingTscOverallLogCount = 3;
    private const int WaitingTscOneLogTimeInMs = 250;
    private const string WaitingTscLogAttemptPrefix = "Log attempt";

    [Fact]
    public async Task Waiting_ForOneRecord()
    {
        // Waiting for one record, fulfilling this wait

        // Arrange
        var testCaseData = new WaitingTestCase(
            1, null, null, false, [
                "Started",
                "Right after starting the background action",
                $"{WaitingTscLogAttemptPrefix} #001",
                "Checking if waiting should end #1",
                "Finished waiting for the log. Waiting for the background action to finish.",
                $"{WaitingTscLogAttemptPrefix} #002",
                $"{WaitingTscLogAttemptPrefix} #003",
                "Background action has finished",
            ],
            false,
            true);

        // Act, Assert
        await WaitingInternal(testCaseData);
    }

    [Fact]
    public async Task Waiting_ForTwoRecords()
    {
        // Waiting for two records, fulfilling this wait

        // Arrange
        var waitingTestCase = new WaitingTestCase(
            2, null, null, false, [
                "Started",
                "Right after starting the background action",
                $"{WaitingTscLogAttemptPrefix} #001",
                "Checking if waiting should end #1",
                $"{WaitingTscLogAttemptPrefix} #002",
                "Checking if waiting should end #2",
                "Finished waiting for the log. Waiting for the background action to finish.",
                $"{WaitingTscLogAttemptPrefix} #003",
                "Background action has finished"
            ],
            false,
            true);

        // Act, Assert
        await WaitingInternal(waitingTestCase);
    }

    [Fact]
    public async Task Waiting_CancellingBeforeWaitFulfilled()
    {
        // Waiting for many log records, but cancelling the wait before this condition is reached at the time of one log record being actually logged.

        // Arrange
        const int OneAndHalfRecordTime = WaitingTscOneLogTimeInMs + (WaitingTscOneLogTimeInMs / 2);
        var waitingTestCase = new WaitingTestCase(
            WaitingTscOverallLogCount + 1, OneAndHalfRecordTime, null, false, [
                "Started",
                "Right after starting the background action",
                $"{WaitingTscLogAttemptPrefix} #001",
                "Checking if waiting should end #1",
                "Finished waiting for the log. Waiting for the background action to finish.",
                $"{WaitingTscLogAttemptPrefix} #002",
                $"{WaitingTscLogAttemptPrefix} #003",
                "Background action has finished"
            ],
            true,
            null);

        // Act, Assert
        await WaitingInternal(waitingTestCase);
    }

    [Fact]
    public async Task Waiting_StartingWaitWithCanceledToken()
    {
        // Waiting for many log records, but starting with cancellation token already cancelled.

        // Arrange
        var waitingTestCase = new WaitingTestCase(
            WaitingTscOverallLogCount, null, null, true, [
                "Started",
                "Right after starting the background action",
                "Finished waiting for the log. Waiting for the background action to finish.",
                $"{WaitingTscLogAttemptPrefix} #001",
                $"{WaitingTscLogAttemptPrefix} #002",
                $"{WaitingTscLogAttemptPrefix} #003",
                "Background action has finished"
            ],
            true,
            null);

        // Act, Assert
        await WaitingInternal(waitingTestCase);
    }

    [Fact]
    public async Task Waiting_WithCancellationAfterWaitFulfilled()
    {
        // Waiting for single log record and supplying a cancellation period that would match three logs to get writer

        // Arrange
        var waitingTestCase = new WaitingTestCase(
            1, 3 * WaitingTscOneLogTimeInMs, null, false, [
                "Started",
                "Right after starting the background action",
                $"{WaitingTscLogAttemptPrefix} #001",
                "Checking if waiting should end #1",
                "Finished waiting for the log. Waiting for the background action to finish.",
                $"{WaitingTscLogAttemptPrefix} #002",
                $"{WaitingTscLogAttemptPrefix} #003",
                "Background action has finished"
            ],
            false,
            true);

        // Act, Assert
        await WaitingInternal(waitingTestCase);
    }

    [Fact]
    public async Task Waiting_TimeoutBeforeWaitIsFulfilled()
    {
        // Waiting for 3 log attempts, but setting timeout to expire after the second attempt.

        // Arrange
        var waitingTestCase = new WaitingTestCase(
            3, null, (2 * WaitingTscOneLogTimeInMs) + (WaitingTscOneLogTimeInMs / 2), false, [
                "Started",
                "Right after starting the background action",
                $"{WaitingTscLogAttemptPrefix} #001",
                "Checking if waiting should end #1",
                $"{WaitingTscLogAttemptPrefix} #002",
                "Checking if waiting should end #2",
                "Finished waiting for the log. Waiting for the background action to finish.",
                $"{WaitingTscLogAttemptPrefix} #003",
                "Background action has finished"
            ],
            false,
            false);

        // Act, Assert
        await WaitingInternal(waitingTestCase);
    }

    private async Task WaitingInternal(WaitingTestCase testCaseData)
    {
        var collector = FakeLogCollector.Create(new FakeLogCollectorOptions());

        var logger = new FakeLogger(collector);

        using var cancellationTokenSource = testCaseData.CancellationWaitInMs is not null
            ? new CancellationTokenSource(TimeSpan.FromMilliseconds(testCaseData.CancellationWaitInMs.Value))
            : new CancellationTokenSource();

        if (testCaseData.StartsWithCancelledToken)
        {
            cancellationTokenSource.Cancel();
        }

        var testExecutionCustomLog = new ConcurrentQueue<string>();

        int count = 0;

        bool EndWaiting(FakeLogRecord record)
        {
            Interlocked.Increment(ref count);
            testExecutionCustomLog.Enqueue("Checking if waiting should end #" + count);
            return count == testCaseData.EndWaitAtAttemptCount;
        }

        // Act

        testExecutionCustomLog.Enqueue("Started");

        TimeSpan? timeout = testCaseData.TimeoutWaitInMs is null
            ? null
            : TimeSpan.FromMilliseconds(testCaseData.TimeoutWaitInMs.Value);

        var awaitingTask = collector.WaitForLogAsync(EndWaiting, timeout, cancellationTokenSource.Token);

        var loggingBackgroundAction = Task.Run(
            () =>
            {
                for (var logAttempt = 1; logAttempt <= WaitingTscOverallLogCount; logAttempt++)
                {
                    Thread.Sleep(WaitingTscOneLogTimeInMs);
                    var message = $"{WaitingTscLogAttemptPrefix} #{logAttempt:000}";
                    testExecutionCustomLog.Enqueue(message);
                    logger.LogDebug(message);
                }
            },
            CancellationToken.None);

        testExecutionCustomLog.Enqueue("Right after starting the background action");

        bool? result = null;
        bool taskCancelled = false;

        try
        {
            result = await awaitingTask;
        }
        catch (TaskCanceledException)
        {
            taskCancelled = true;
        }

        testExecutionCustomLog.Enqueue("Finished waiting for the log. Waiting for the background action to finish.");

        await loggingBackgroundAction;

        testExecutionCustomLog.Enqueue("Background action has finished");

        // Assert
        Assert.Equal(result, testCaseData.ExpectedAwaitedTaskResult);
        Assert.True(testExecutionCustomLog.SequenceEqual(testCaseData.ExpectedOperationSequence));
        Assert.Equal(testExecutionCustomLog.Count(r => r.StartsWith(WaitingTscLogAttemptPrefix)), logger.Collector.Count);
        Assert.Equal(taskCancelled, testCaseData.ExpectedTaskCancelled);
    }
}
