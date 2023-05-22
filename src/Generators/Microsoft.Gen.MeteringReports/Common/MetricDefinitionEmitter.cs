// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Gen.Metering.Model;
using Microsoft.Gen.Shared;

namespace Microsoft.Gen.MeteringReports;

// Stryker disable all

internal static class MetricDefinitionEmitter
{
    private static readonly StringBuilderPool _builders = new();

    public static string GenerateReport(IReadOnlyList<ReportedMetricClass> metricClasses, CancellationToken cancellationToken)
    {
        if (metricClasses == null || metricClasses.Count == 0)
        {
            return string.Empty;
        }

        var sb = _builders.GetStringBuilder();
        try
        {
            _ = sb.Append('[')
                  .Append('\n');

            for (int i = 0; i < metricClasses.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var metricClass = metricClasses[i];
                _ = sb.Append(GenMetricClassDefinition(metricClass, cancellationToken));

                if (i < metricClasses.Count - 1)
                {
                    _ = sb.Append(',');
                }

                _ = sb.Append('\n');
            }

            _ = sb.Append(']');

            return sb.ToString();
        }
        finally
        {
            _builders.ReturnStringBuilder(sb);
        }
    }

    private static string GenMetricClassDefinition(ReportedMetricClass metricClass, CancellationToken cancellationToken)
    {
        var sb = _builders.GetStringBuilder();
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            _ = sb.Append(" {")
                  .Append('\n');

            _ = sb.Append($"  \"{metricClass.RootNamespace}\":");
            _ = sb.Append('\n');

            if (metricClass.Methods.Length > 0)
            {
                _ = sb.Append("  [")
                      .Append('\n');

                for (int j = 0; j < metricClass.Methods.Length; j++)
                {
                    var metricMethod = metricClass.Methods[j];

                    _ = sb.Append(GenMetricMethodDefinition(metricMethod, cancellationToken));

                    if (j < metricClass.Methods.Length - 1)
                    {
                        _ = sb.Append(',');
                    }

                    _ = sb.Append('\n');
                }

                _ = sb.Append("  ]")
                      .Append('\n');
            }

            _ = sb.Append(" }");

            return sb.ToString();
        }
        finally
        {
            _builders.ReturnStringBuilder(sb);
        }
    }

    private static string GenMetricMethodDefinition(ReportedMetricMethod metricMethod, CancellationToken cancellationToken)
    {
        switch (metricMethod.Kind)
        {
            case InstrumentKind.Counter:
            case InstrumentKind.Histogram:
            case InstrumentKind.Gauge:
                var sb = _builders.GetStringBuilder();
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    _ = sb.Append("    {")
                          .Append('\n');

                    _ = sb.Append($"     \"MetricName\": \"{metricMethod.MetricName.Replace("\\", "\\\\").Replace("\"", "\\\"")}\",")
                          .Append('\n');

                    if (!string.IsNullOrEmpty(metricMethod.Summary))
                    {
                        _ = sb.Append($"     \"MetricDescription\": \"{metricMethod.Summary.Replace("\\", "\\\\").Replace("\"", "\\\"")}\",");
                        _ = sb.Append('\n');
                    }

                    _ = sb.Append($"     \"InstrumentName\": \"{metricMethod.Kind}\"");

                    if (metricMethod.Dimensions.Count > 0)
                    {
                        _ = sb.Append(',');
                        _ = sb.Append('\n');
                        _ = sb.Append("     \"Dimensions\": {");

                        int k = 0;

                        foreach (var dimension in metricMethod.Dimensions)
                        {
                            _ = sb.Append('\n');
                            if (metricMethod.DimensionsDescriptions.TryGetValue(dimension, out var description))
                            {
                                _ = sb.Append($"      \"{dimension}\": \"{description.Replace("\\", "\\\\").Replace("\"", "\\\"")}\"");
                            }
                            else
                            {
                                _ = sb.Append($"      \"{dimension}\": \"\"");
                            }

                            if (k < metricMethod.Dimensions.Count - 1)
                            {
                                _ = sb.Append(',');
                            }

                            k++;
                        }

                        _ = sb.Append('\n');
                        _ = sb.Append("      }");
                        _ = sb.Append('\n');
                    }
                    else
                    {
                        _ = sb.Append('\n');
                    }

                    _ = sb.Append("    }");

                    return sb.ToString();
                }
                catch (Exception e)
                {
                    // This should report diagnostic.
                    throw new InvalidOperationException($"An exception occurred during metric report generation {e.GetType()}:{e.Message}.");
                }
                finally
                {
                    _builders.ReturnStringBuilder(sb);
                }

            case InstrumentKind.None:
            case InstrumentKind.CounterT:
            case InstrumentKind.HistogramT:
            default:
                // This should report diagnostic.
                throw new NotSupportedException($"Report for metric kind: '{metricMethod.Kind}' is not supported.");
        }
    }
}
