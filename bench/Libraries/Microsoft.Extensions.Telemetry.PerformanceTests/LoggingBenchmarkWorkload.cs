// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.Telemetry.Bench;

internal static class LoggingBenchmarkWorkload
{
    public const string CriticalCategoryPrefix = "Benchmark.Critical.";
    public const string HighVolumeCategoryPrefix = "Benchmark.HighVolume.";

    private const int EventCount = 16;

    private static readonly string[] _categories =
    [
        $"{HighVolumeCategoryPrefix}Orders",
        $"{HighVolumeCategoryPrefix}Payments",
        $"{CriticalCategoryPrefix}Audit",
        "Benchmark.Diagnostics"
    ];

    private static readonly Action<ILogger, int, Exception?>[] _messages = CreateMessages();

    public static ILogger[] CreateLoggers(ILoggerFactory factory)
    {
        var loggers = new ILogger[_categories.Length];
        for (int i = 0; i < loggers.Length; i++)
        {
            loggers[i] = factory.CreateLogger(_categories[i]);
        }

        return loggers;
    }

    public static void LogBatch(ILogger[] loggers, int recordCount)
    {
        for (int i = 0; i < recordCount; i++)
        {
            int eventIndex = SelectEventIndex(i);
            int categoryIndex = (i / 100) % loggers.Length;
            _messages[eventIndex](loggers[categoryIndex], i, null);
        }
    }

    private static Action<ILogger, int, Exception?>[] CreateMessages()
    {
        var messages = new Action<ILogger, int, Exception?>[EventCount];
        for (int i = 0; i < messages.Length; i++)
        {
            messages[i] = LoggerMessage.Define<int>(
                LogLevel.Information,
                new EventId(i + 1, $"BenchmarkEvent{i + 1}"),
                "Benchmark message {Value}");
        }

        return messages;
    }

    private static int SelectEventIndex(int recordIndex)
    {
        int percentile = recordIndex % 100;
        if (percentile < 70)
        {
            return 0;
        }

        if (percentile < 85)
        {
            return 1;
        }

        if (percentile < 93)
        {
            return 2;
        }

        return 3 + (recordIndex % (EventCount - 3));
    }
}
