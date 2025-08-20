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
    private readonly ITestOutputHelper _outputHelper;

    public FakeLogCollectorTests(ITestOutputHelper outputHelper)
    {
        _outputHelper = outputHelper;
    }

    [Fact]
    public async Task JustSomeTest()
    {

    }

    [Fact]
    public async Task Test()
    {
        var collector = FakeLogCollector.Create(new FakeLogCollectorOptions());
        var logger = new FakeLogger(collector);

        _ = Task.Run(async () =>
        {
            int i = 0;
            while (i < 21)
            {
                logger.Log(LogLevel.Critical, $"Item {i}");
                _outputHelper.WriteLine($"Written item: Item {i} at {DateTime.Now}, currently items: {logger.Collector.Count}");

                await Task.Delay(3_000, CancellationToken.None);
                i++;
            }
        });

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

        var toCollect2 = new HashSet<string> {"Item 20",};
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
    }
}
