// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
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

    [Fact]
    public async Task Test()
    {
        var collector = FakeLogCollector.Create(new FakeLogCollectorOptions());

        var logEmittingTask = EmitLogs(collector, 20, TimeSpan.FromMilliseconds(1_000));

        var toCollect = new HashSet<string> {"Item 1", "Item 4", "Item 9",};
        var collected = new HashSet<string>();

        await foreach (var log in collector.GetLogsAsync(0, null, CancellationToken.None))
        {
            _outputHelper.WriteLine($"-- Got new item: {log.Message} at {DateTime.Now}");

            if (toCollect.Contains(log.Message))
            {
                _outputHelper.WriteLine($"⏹️ Collected item: {log.Message} at {DateTime.Now}");
                collected.Add(log.Message);
            }

            if (collected.Count == toCollect.Count)
            {
                break;
            }
        }

        _outputHelper.WriteLine($"------ Finished waiting for the first set at {DateTime.Now}");

        //await Task.Delay(20_000);

        var toCollect2 = new HashSet<string> {"Item 17",};
        var collected2 = new HashSet<string>();

        await foreach (var log in collector.GetLogsAsync(3, null, CancellationToken.None))
        {
            _outputHelper.WriteLine($"Got new item: {log.Message} at {DateTime.Now}");

            if (toCollect2.Contains(log.Message))
            {
                _outputHelper.WriteLine($"⏹️ Collected item: {log.Message} at {DateTime.Now}");
                collected2.Add(log.Message);
            }

            if (collected2.Count == toCollect2.Count)
            {
                break;
            }
        }

        _outputHelper.WriteLine($"------ Finished waiting for the second set at {DateTime.Now}");

        await logEmittingTask;
    }

    [Fact]
    public async Task Test2()
    {
        var collector = FakeLogCollector.Create(new FakeLogCollectorOptions());

        var logEmittingTask = EmitLogs(collector, 20, TimeSpan.FromMilliseconds(1_000));

        var toCollect = new HashSet<string> {"Item 1", "Item 4", "Item 9",};
        var collected = new HashSet<string>();

        var index = await collector.WaitForLogsAsync(log =>
        {
            _outputHelper.WriteLine($"-- Got new item: {log.Message} at {DateTime.Now}");

            if (toCollect.Contains(log.Message))
            {
                _outputHelper.WriteLine($"⏹️ Collected item: {log.Message} at {DateTime.Now}");
                collected.Add(log.Message);
            }

            if (collected.Count == toCollect.Count)
            {
                return true;
            }

            return false;
        });

        _outputHelper.WriteLine($"------ Finished waiting for the first set at {DateTime.Now}. Got index: {index}");

        //await Task.Delay(20_000);

        var toCollect2 = new HashSet<string> {"Item 9999",};
        var collected2 = new HashSet<string>();

        var waitingForLogs = collector.WaitForLogsAsync(log =>
        {
            _outputHelper.WriteLine($"Got new item: {log.Message} at {DateTime.Now}");

            if (toCollect2.Contains(log.Message))
            {
                _outputHelper.WriteLine($"⏹️ Collected item: {log.Message} at {DateTime.Now}");
                collected2.Add(log.Message);
            }

            if (collected2.Count == toCollect2.Count)
            {
                return true;
            }

            return false;
        }, startingIndex: index + 1, timeout: TimeSpan.FromMilliseconds(3_000));

        try
        {
            await waitingForLogs;
        }
        catch (OperationCanceledException)
        {
            _outputHelper.WriteLine($"Waiting exception!!!!!!! at {DateTime.Now}");
        }

        _outputHelper.WriteLine($"------ Finished waiting for the second set at {DateTime.Now}");

        await logEmittingTask;
    }

    private async Task EmitLogs(FakeLogCollector fakeLogCollector, int count, TimeSpan? delayBetweenEmissions = null)
    {
        var logger = new FakeLogger(fakeLogCollector);

        await Task.Run(async () =>
        {
            int i = 0;
            while (i < count)
            {
                logger.Log(LogLevel.Debug, $"Item {i}");
                _outputHelper.WriteLine($"Written item: Item {i} at {DateTime.Now}, currently items: {logger.Collector.Count}");

                if (delayBetweenEmissions.HasValue)
                {
                    await Task.Delay(delayBetweenEmissions.Value, CancellationToken.None);
                }

                i++;
            }
        });

        _outputHelper.WriteLine($"Finished emitting logs at {DateTime.Now}");
    }
}
