// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Time.Testing;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Extensions.Logging.Testing.Test.Logging;

public partial class FakeLogCollectorTests
{
    private record WaitingTestCase(
        int EndWaitAtAttemptCount,
        double? CancellationAfterLogAttemptCount,
        double? TimeoutAfterLogAttemptCount,
        bool StartsWithCancelledToken,
        string[] ExpectedOperationSequence,
        bool ExpectedTaskCancelled,
        bool? ExpectedAwaitedTaskResult
    );

    private const int WaitingTscOverallLogCount = 3;
    private const int LogAttemptHalfTimePeriod = 250;
    private const int LogAttemptFullTimePeriod = LogAttemptHalfTimePeriod * 2;

    /* Testing infrastructure messages */
    private const string StartedWaitingMessage = "Started awaiting";
    private const string AfterStartingBackgroundActionMessage = "Right after starting the background action";
    private const string CheckingWaitingCallbackMessagePrefix = "Checking if waiting should end";
    private const string BackgroundActionFinishedMessage = "Background action has finished";
    private const string FinishedWaitingMessage = "Finished waiting for the log. Waiting for the background action to finish.";

    private const string AwaitedLogMessagePrefix = "Awaited log message";
    private const string IrrelevantLogMessage = "Irrelevant log message";

    [Fact]
    public async Task Waiting_ForOneRecord()
    {
        // Waiting for one record, fulfilling this wait

        // Arrange
        var testCaseData = new WaitingTestCase(
            1, null, null, false, [
                IrrelevantLogMessage, // log message before start of waiting
                StartedWaitingMessage,
                $"{CheckingWaitingCallbackMessagePrefix} #001", // condition is checked at the waiting start if not immediately cancelled
                AfterStartingBackgroundActionMessage,
                IrrelevantLogMessage, // different message logged each half period
                $"{CheckingWaitingCallbackMessagePrefix} #002",
                $"{AwaitedLogMessagePrefix} #001",
                $"{CheckingWaitingCallbackMessagePrefix} #003",
                FinishedWaitingMessage,
                IrrelevantLogMessage, // different message logged each half period
                $"{AwaitedLogMessagePrefix} #002",
                IrrelevantLogMessage, // different message logged each half period
                $"{AwaitedLogMessagePrefix} #003",
                BackgroundActionFinishedMessage,
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
                IrrelevantLogMessage, // log message before start of waiting
                StartedWaitingMessage,
                $"{CheckingWaitingCallbackMessagePrefix} #001", // condition is checked at the waiting start if not immediately cancelled
                AfterStartingBackgroundActionMessage,
                IrrelevantLogMessage, // different message logged each half period
                $"{CheckingWaitingCallbackMessagePrefix} #002",
                $"{AwaitedLogMessagePrefix} #001",
                $"{CheckingWaitingCallbackMessagePrefix} #003",
                IrrelevantLogMessage, // different message logged each half period
                $"{CheckingWaitingCallbackMessagePrefix} #004",
                $"{AwaitedLogMessagePrefix} #002",
                $"{CheckingWaitingCallbackMessagePrefix} #005",
                FinishedWaitingMessage,
                IrrelevantLogMessage, // different message logged each half period
                $"{AwaitedLogMessagePrefix} #003",
                BackgroundActionFinishedMessage,
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
        var waitingTestCase = new WaitingTestCase(
            WaitingTscOverallLogCount + 1, 1.5, null, false, [
                IrrelevantLogMessage, // log message before start of waiting
                StartedWaitingMessage,
                $"{CheckingWaitingCallbackMessagePrefix} #001", // condition is checked at the waiting start if not immediately cancelled
                AfterStartingBackgroundActionMessage,
                IrrelevantLogMessage, // different message logged each half period
                $"{CheckingWaitingCallbackMessagePrefix} #002",
                $"{AwaitedLogMessagePrefix} #001",
                $"{CheckingWaitingCallbackMessagePrefix} #003",
                IrrelevantLogMessage, // different message logged each half period
                FinishedWaitingMessage,
                $"{AwaitedLogMessagePrefix} #002",
                IrrelevantLogMessage, // different message logged each half period
                $"{AwaitedLogMessagePrefix} #003",
                BackgroundActionFinishedMessage
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
                IrrelevantLogMessage, // log message before start of waiting
                StartedWaitingMessage,
                AfterStartingBackgroundActionMessage,
                FinishedWaitingMessage,
                IrrelevantLogMessage, // different message logged each half period
                $"{AwaitedLogMessagePrefix} #001",
                IrrelevantLogMessage, // different message logged each half period
                $"{AwaitedLogMessagePrefix} #002",
                IrrelevantLogMessage, // different message logged each half period
                $"{AwaitedLogMessagePrefix} #003",
                BackgroundActionFinishedMessage
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
            1, 3 * LogAttemptFullTimePeriod, null, false, [
                IrrelevantLogMessage, // log message before start of waiting
                StartedWaitingMessage,
                $"{CheckingWaitingCallbackMessagePrefix} #001", // condition is checked at the waiting start if not immediately cancelled
                AfterStartingBackgroundActionMessage,
                IrrelevantLogMessage, // different message logged each half period
                $"{CheckingWaitingCallbackMessagePrefix} #002",
                $"{AwaitedLogMessagePrefix} #001",
                $"{CheckingWaitingCallbackMessagePrefix} #003",
                FinishedWaitingMessage,
                IrrelevantLogMessage, // different message logged each half period
                $"{AwaitedLogMessagePrefix} #002",
                IrrelevantLogMessage, // different message logged each half period
                $"{AwaitedLogMessagePrefix} #003",
                BackgroundActionFinishedMessage,
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
            3, null, 2.5, false, [
                IrrelevantLogMessage, // log message before start of waiting
                StartedWaitingMessage,
                $"{CheckingWaitingCallbackMessagePrefix} #001",
                AfterStartingBackgroundActionMessage,
                IrrelevantLogMessage, // different message logged each half period
                $"{CheckingWaitingCallbackMessagePrefix} #002",
                $"{AwaitedLogMessagePrefix} #001",
                $"{CheckingWaitingCallbackMessagePrefix} #003",
                IrrelevantLogMessage, // different message logged each half period
                $"{CheckingWaitingCallbackMessagePrefix} #004",
                $"{AwaitedLogMessagePrefix} #002",
                $"{CheckingWaitingCallbackMessagePrefix} #005",
                IrrelevantLogMessage, // different message logged each half period
                FinishedWaitingMessage,
                $"{AwaitedLogMessagePrefix} #003",
                BackgroundActionFinishedMessage
            ],
            false,
            false);

        // Act, Assert
        await WaitingInternal(waitingTestCase);
    }

    private async Task WaitingInternal(WaitingTestCase testCaseData)
    {
        // Testing infrastructure: capping the time of the test run
        bool stoppedByInfrastructure = false;
        const int TestRunCapMs = 2_000;
        using var circuitBreakerCts = new CancellationTokenSource();
        var testInfrastructureCircuitBreakerTask = Task.Run(async () =>
        {
            await Task.Delay(TestRunCapMs, CancellationToken.None);
            stoppedByInfrastructure = true;
            return false;
        }, circuitBreakerCts.Token);

        // Wrapped test case run
        await await Task.WhenAny(WaitingInternalCore(testCaseData), testInfrastructureCircuitBreakerTask);

        // Test infrastructure assert
        Assert.False(stoppedByInfrastructure, "None of the test cases is expected to reach test infrastructure timeout");
        circuitBreakerCts.Cancel(); // avoid resource leaking when core task finishes first
    }

    private async Task WaitingInternalCore(WaitingTestCase testCaseData)
    {
        // Testing infrastructure: keeping track of when the implementation checks the users callback
        var testExecutionCustomLog = new ConcurrentQueue<string>();
        int callbackCallCounter = 0;
        int awaitedLogMessageCount = 0;
        const int AllLogAttemptsInHalfPeriods = WaitingTscOverallLogCount * 2;
        var fakeTimeProvider = new FakeTimeProvider();

        var collector = FakeLogCollector.Create(new FakeLogCollectorOptions { TimeProvider = fakeTimeProvider });
        var logger = new FakeLogger(collector);

        bool UserDefinedWaitingCondition(FakeLogRecord record)
        {
            callbackCallCounter++;
            testExecutionCustomLog.Enqueue($"{CheckingWaitingCallbackMessagePrefix} #{callbackCallCounter:000}");

            if (record.Message.StartsWith(AwaitedLogMessagePrefix))
            {
                awaitedLogMessageCount++;
            }

            return awaitedLogMessageCount == testCaseData.EndWaitAtAttemptCount;
        }

        TimeSpan? userDefinedTimeout = testCaseData.TimeoutAfterLogAttemptCount is null
            ? null
            : TimeSpan.FromMilliseconds(LogAttemptFullTimePeriod * testCaseData.TimeoutAfterLogAttemptCount.Value);

        var userDefinedCancellation = testCaseData.CancellationAfterLogAttemptCount.HasValue
            ? CreateCtsWithTimeProvider(TimeSpan.FromMilliseconds(LogAttemptFullTimePeriod * testCaseData.CancellationAfterLogAttemptCount.Value), fakeTimeProvider)
            : new CancellationTokenSource();

        using (userDefinedCancellation)
        {
            if (testCaseData.StartsWithCancelledToken)
            {
#if NET8_0_OR_GREATER
                await CancelTokenSource(userDefinedCancellation);
#else
                CancelTokenSource(userDefinedCancellation);
#endif
            }

            // Act
            logger.LogDebug(IrrelevantLogMessage);
            testExecutionCustomLog.Enqueue(IrrelevantLogMessage);

            testExecutionCustomLog.Enqueue("Started awaiting");

            var awaitingTask = collector.WaitForLogAsync(UserDefinedWaitingCondition, userDefinedTimeout, userDefinedCancellation.Token);

            var loggingBackgroundAction = Task.Run(
                async () =>
                {
                    // Simulating time progression in half-time periods to allow for mid-period timeout waiting completions
                    for (var logAttemptHalfPeriodCount = 1; logAttemptHalfPeriodCount <= AllLogAttemptsInHalfPeriods; logAttemptHalfPeriodCount++)
                    {
                        await Task.Delay(100, CancellationToken.None); // even though the test is not real time-based, we need to give room for the parallel post task completion operations
                        fakeTimeProvider.Advance(TimeSpan.FromMilliseconds(LogAttemptHalfTimePeriod));

                        var logAttemptPeriodReached = logAttemptHalfPeriodCount % 2 == 0;
                        if (logAttemptPeriodReached)
                        {
                            var logAttemptNumber = logAttemptHalfPeriodCount / 2;
                            var logMessage = $"{AwaitedLogMessagePrefix} #{logAttemptNumber:000}";
                            testExecutionCustomLog.Enqueue(logMessage); // Testing infrastructure log
                            logger.LogDebug(logMessage); // Actual log
                        }
                        else
                        {
                            testExecutionCustomLog.Enqueue(IrrelevantLogMessage); // Testing infrastructure log
                            logger.LogDebug(IrrelevantLogMessage);
                        }
                    }
                },
                CancellationToken.None);

            testExecutionCustomLog.Enqueue(AfterStartingBackgroundActionMessage);

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

            testExecutionCustomLog.Enqueue(FinishedWaitingMessage);

            await loggingBackgroundAction;

            testExecutionCustomLog.Enqueue(BackgroundActionFinishedMessage);

            // Assert
            Assert.Equal(testCaseData.ExpectedAwaitedTaskResult, result);
            testExecutionCustomLog.Should().Equal(testCaseData.ExpectedOperationSequence);
            Assert.Equal(
                testExecutionCustomLog.Count(r => r.StartsWith(AwaitedLogMessagePrefix)),
                logger.Collector.GetSnapshot().Count(r => r.Message.StartsWith(AwaitedLogMessagePrefix))
            );
            Assert.Equal(testCaseData.ExpectedTaskCancelled, taskCancelled);
        }
    }

#if NET8_0_OR_GREATER
    async Task CancelTokenSource(CancellationTokenSource cts) => await cts.CancelAsync();
#else
        void CancelTokenSource(CancellationTokenSource cts) => cts.Cancel();
#endif

    CancellationTokenSource CreateCtsWithTimeProvider(TimeSpan timeSpan, TimeProvider timeProvider)
    {
#if NET8_0_OR_GREATER
        return new CancellationTokenSource(timeSpan, timeProvider);
#else
        return timeProvider.CreateCancellationTokenSource(timeSpan);
#endif
    }
}
