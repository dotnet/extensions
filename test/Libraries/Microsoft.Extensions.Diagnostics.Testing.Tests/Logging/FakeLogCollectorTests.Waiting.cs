// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Extensions.Logging.Testing.Test.Logging;

public partial class FakeLogCollectorTests
{
    private readonly ITestOutputHelper _outputHelper;

    public FakeLogCollectorTests(ITestOutputHelper outputHelper)
    {
        _outputHelper = outputHelper;
    }

    [Theory]
    [InlineData(true, false, false)]
    [InlineData(true, false, true)]
    [InlineData(false, true, false)]
    public async Task LogAwaitingDemo(bool arrivesInAwaitedOrder, bool expectedToCancel, bool useClear)
    {
        var collector = FakeLogCollector.Create(new FakeLogCollectorOptions());
        var eventTracker = new ConcurrentQueue<string>();

        var waitingTimeout = TimeSpan.FromMilliseconds(1_000);

        string[] logsToEmit = arrivesInAwaitedOrder
            ? ["Sync", "Log A", "Sync", "Sync", "Log B", "Sync", "Sync", "Log C", "Sync", "Sync", "Sync"]
            : ["Sync", "Log A", "Sync", "Sync", "Log B", "Sync", "Sync", "Log C", "Log D"] // Log C is not followed by Sync
        ;

        var logEmittingTask = EmitLogs(collector, logsToEmit, eventTracker);

        var res = await AwaitSequence(
            new Queue<string>(["Log A", "Log B", "Sync"]), // Wait for event A and B followed by Sync
            fromIndex: 0,
            collector,
            eventTracker,
            timeout: waitingTimeout);

        Assert.False(res.wasCancelled);
        Assert.Equal(5, res.index);

        if (useClear)
        {
            var countToClear = res.index + 1;
            eventTracker.Enqueue($"Clearing collector by {countToClear} items at {DateTime.Now}");
            collector.Clear(countToClear);
        }

        // This gap simulates an action on the tested running code expected to trigger event C followed by Sync
        await Task.Delay(2_000);

        res = await AwaitSequence(
            new Queue<string>(["Log C", "Sync"]), // Wait for Log C followed by Sync
            fromIndex: useClear ? 0 : res.index + 1,
            collector,
            eventTracker,
            timeout: waitingTimeout);

        Assert.Equal(expectedToCancel, res.wasCancelled);
        Assert.Equal(expectedToCancel ? -1 : (useClear ? 2 : 8), res.index);

        await logEmittingTask;

        if (!useClear && !expectedToCancel)
        {
            // The user may want to await partial states to perform actions, but then perform a sanity check on the whole history
            var snapshot = collector.GetSnapshot();

            var expectedProgression = new Queue<string>(["Log A", "Log B", "Sync", "Log C", "Sync"]);
            foreach (var item in snapshot.Select(x => x.Message))
            {
                if (expectedProgression.Count == 0)
                {
                    break;
                }

                if (item == expectedProgression.Peek())
                {
                    expectedProgression.Dequeue();
                }
            }

            Assert.Empty(expectedProgression);
        }

        OutputEventTracker(_outputHelper, eventTracker);
    }

    private static async Task<(bool wasCancelled, int index)> AwaitSequence(
        Queue<string> sequence,
        int fromIndex,
        FakeLogCollector collector,
        ConcurrentQueue<string> eventTracker,
        TimeSpan? timeout = null)
    {
        try
        {
            int index = fromIndex - 1;
            var enumeration = collector.GetLogsAsync(startingIndex: fromIndex, timeout: timeout);
            await foreach (var log in enumeration)
            {
                index++;

                var msg = log.Message;
                var currentExpectation = sequence.Peek();

                eventTracker.Enqueue($"Checking log: \"{msg}\".");

                if (msg == currentExpectation)
                {
                    sequence.Dequeue();

                    if (sequence.Count != 0)
                    {
                        continue;
                    }

                    eventTracker.Enqueue($"Sequence satisfied at {DateTime.Now}");

                    return (false, index);
                }
            }
        }
        catch (OperationCanceledException)
        {
            eventTracker.Enqueue($"Operation cancelled at {DateTime.Now}");
            return (true, -1);
        }

        throw new Exception("Enumeration was supposed to be unbound.");
    }

    private static void OutputEventTracker(ITestOutputHelper testOutputHelper, ConcurrentQueue<string> eventTracker)
    {
        while (eventTracker.TryDequeue(out var item))
        {
            testOutputHelper.WriteLine(item);
        }
    }

    private async Task EmitLogs(
        FakeLogCollector fakeLogCollector,
        IEnumerable<string> logsToEmit,
        ConcurrentQueue<string> eventTracker,
        TimeSpan? delayBetweenEmissions = null)
    {
        var logger = new FakeLogger(fakeLogCollector);

        await Task.Run(async () =>
        {
            eventTracker.Enqueue($"Started emitting logs at {DateTime.Now}");

            foreach(var log in logsToEmit)
            {
                eventTracker.Enqueue($"Emitting item: \"{log}\" at {DateTime.Now}, currently items: {logger.Collector.Count}");
                logger.Log(LogLevel.Debug, log);

                if (delayBetweenEmissions.HasValue)
                {
                    await Task.Delay(delayBetweenEmissions.Value, CancellationToken.None);
                }
            }
        });

        eventTracker.Enqueue($"Finished emitting logs at {DateTime.Now}");
    }
}
