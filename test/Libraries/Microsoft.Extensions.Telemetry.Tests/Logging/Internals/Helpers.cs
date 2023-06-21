// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.Extensions.Telemetry.Logging.Test.Internals;

internal static class Helpers
{
    public static bool TryGetStackTrace(this IReadOnlyCollection<KeyValuePair<string, object>> stateValues, out string? stackTrace)
    {
        if (stateValues.Count < 1)
        {
            stackTrace = null;
            return false;
        }

        foreach (var entry in stateValues)
        {
            if (entry.Key == "stackTrace")
            {
                stackTrace = entry.Value.ToString();
                return true;
            }
        }

        stackTrace = null;
        return false;
    }

    public static bool CompareStateValues(IReadOnlyCollection<KeyValuePair<string, object>> stateValues, Dictionary<string, object> dictExpected)
    {
        if (stateValues.Count != dictExpected.Count)
        {
            return false;
        }

        foreach (var entry in stateValues)
        {
            if (dictExpected.TryGetValue(entry.Key, out var value))
            {
                if (entry.Value is null || value is null)
                {
                    if (entry.Value is null && value is null)
                    {
                        return true;
                    }

                    return false;
                }

                if (entry.Value!.ToString() != value!.ToString())
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        return true;
    }

#if false
    public static ILogger CreateLogger(Action<ILoggingBuilder> configure, BaseExporter<LogRecord> exporter)
    {
        var hostBuilder = FakeHost.CreateBuilder(options => options.FakeLogging = false)
            .ConfigureLogging(builder =>
            {
                configure.Invoke(builder);
                _ = builder.Services.AddExtendedLogging();
            });

        var host = hostBuilder.Build();
        var logger = host.Services.GetRequiredService<ILogger<LogEnrichmentTests>>();

        return logger;
    }
#endif
}
