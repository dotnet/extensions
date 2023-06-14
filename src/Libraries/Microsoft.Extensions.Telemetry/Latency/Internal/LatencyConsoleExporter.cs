// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;
using Microsoft.Shared.Memoization;
using Microsoft.Shared.Pools;
using Microsoft.Shared.Text;
using CF = Microsoft.Shared.Text.CompositeFormat;

namespace Microsoft.Extensions.Telemetry.Latency.Internal;

internal sealed class LatencyConsoleExporter : ILatencyDataExporter
{
    private const int MillisPerSecond = 1000;

    private static readonly CF _title = CF.Parse("Latency sample #{0}: {1}ms, {2} checkpoints, {3} tags, {4} measures" + Environment.NewLine);
    private static readonly Func<int, CF> _rows = Memoize.Function<int, CF>(nameColumnWidth => CF.Parse($"  {{0,-{nameColumnWidth}}} | {{1}}" + Environment.NewLine));
    private static readonly Func<int, string> _dashes = Memoize.Function<int, string>(num => new('-', num));

    private readonly bool _outputCheckpoints;
    private readonly bool _outputTags;
    private readonly bool _outputMeasures;
    private long _sampleCount = -1;

    public LatencyConsoleExporter(IOptions<LatencyConsoleOptions> options)
    {
        var o = options.Value;
        _outputCheckpoints = o.OutputCheckpoints;
        _outputTags = o.OutputTags;
        _outputMeasures = o.OutputMeasures;
    }

    public Task ExportAsync(LatencyData latencyData, CancellationToken cancellationToken)
    {
        var sb = PoolFactory.SharedStringBuilderPool.Get();
        try
        {
            var cnt = Interlocked.Increment(ref _sampleCount);

            _ = sb.AppendFormat(
                _title,
                null,
                cnt,
                (double)latencyData.DurationTimestamp / latencyData.DurationTimestampFrequency * MillisPerSecond,
                latencyData.Checkpoints.Length,
                latencyData.Tags.Length,
                latencyData.Measures.Length);

            bool needBlankLine = false;
            if (_outputCheckpoints && latencyData.Checkpoints.Length > 0)
            {
                int nameColumnWidth = 0;
                for (int i = 0; i < latencyData.Checkpoints.Length; i++)
                {
                    nameColumnWidth = Math.Max(nameColumnWidth, latencyData.Checkpoints[i].Name.Length);
                }

                var fmt = StartTable(sb, "Checkpoint", "Value (ms)", nameColumnWidth, ref needBlankLine);
                for (int i = 0; i < latencyData.Checkpoints.Length; i++)
                {
                    var c = latencyData.Checkpoints[i];
                    _ = sb.AppendFormat(fmt, null, c.Name, (double)c.Elapsed / c.Frequency * MillisPerSecond);
                }
            }

            if (_outputTags && latencyData.Tags.Length > 0)
            {
                int nameColumnWidth = 0;
                for (int i = 0; i < latencyData.Tags.Length; i++)
                {
                    nameColumnWidth = Math.Max(nameColumnWidth, latencyData.Tags[i].Name.Length);
                }

                var fmt = StartTable(sb, "Tag", "Value", nameColumnWidth, ref needBlankLine);
                for (int i = 0; i < latencyData.Tags.Length; i++)
                {
                    var t = latencyData.Tags[i];
                    _ = sb.AppendFormat(fmt, null, t.Name, t.Value);
                }
            }

            if (_outputMeasures && latencyData.Measures.Length > 0)
            {
                int nameColumnWidth = 0;
                for (int i = 0; i < latencyData.Measures.Length; i++)
                {
                    nameColumnWidth = Math.Max(nameColumnWidth, latencyData.Measures[i].Name.Length);
                }

                var fmt = StartTable(sb, "Measure", "Value", nameColumnWidth, ref needBlankLine);
                for (int i = 0; i < latencyData.Measures.Length; i++)
                {
                    var m = latencyData.Measures[i];
                    _ = sb.AppendFormat(fmt, null, m.Name, m.Value);
                }
            }

            // the whole sample is output in a single shot so it won't be interrupted with conflicting output
            return Console.Out.WriteAsync(sb.ToString());
        }
        finally
        {
            PoolFactory.SharedStringBuilderPool.Return(sb);
        }
    }

    private static CF StartTable(StringBuilder sb, string nameHeader, string valueHeader, int nameColumnWidth, ref bool needBlankLine)
    {
        if (needBlankLine)
        {
            _ = sb.AppendLine();
        }
        else
        {
            needBlankLine = true;
        }

        nameColumnWidth = Math.Max(nameColumnWidth, nameHeader.Length);
        var fmt = _rows(nameColumnWidth);
        _ = sb.AppendFormat(fmt, null, nameHeader, valueHeader);

        _ = sb.Append("  ");
        _ = sb.Append(_dashes(nameColumnWidth + 1));
        _ = sb.Append('|');
        _ = sb.AppendLine(_dashes(valueHeader.Length + 1));

        return fmt;
    }
}
