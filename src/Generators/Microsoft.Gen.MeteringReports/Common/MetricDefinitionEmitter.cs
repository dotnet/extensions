// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Gen.Metering.Model;
using Microsoft.Gen.Shared;

namespace Microsoft.Gen.MeteringReports;

// Stryker disable all

internal sealed class MetricDefinitionEmitter : EmitterBase
{
    internal MetricDefinitionEmitter()
        : base(false)
    {
    }

    public string GenerateReport(IReadOnlyList<ReportedMetricClass> metricClasses, CancellationToken cancellationToken)
    {
        if (metricClasses == null || metricClasses.Count == 0)
        {
            return string.Empty;
        }

        OutLn("[");

        for (int i = 0; i < metricClasses.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var metricClass = metricClasses[i];
            GenMetricClassDefinition(metricClass, cancellationToken);

            if (i < metricClasses.Count - 1)
            {
                Out(",");
            }

            OutLn();
        }

        Out("]");
        return Capture();
    }

    private void GenMetricClassDefinition(ReportedMetricClass metricClass, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        OutLn(" {");

        OutLn($"  \"{metricClass.RootNamespace}\":");

        if (metricClass.Methods.Length > 0)
        {
            OutLn("  [");

            for (int j = 0; j < metricClass.Methods.Length; j++)
            {
                var metricMethod = metricClass.Methods[j];

                GenMetricMethodDefinition(metricMethod, cancellationToken);

                if (j < metricClass.Methods.Length - 1)
                {
                    Out(",");
                }

                OutLn();
            }

            OutLn("  ]");
        }

        Out(" }");
    }

    private void GenMetricMethodDefinition(ReportedMetricMethod metricMethod, CancellationToken cancellationToken)
    {
        switch (metricMethod.Kind)
        {
            case InstrumentKind.Counter:
            case InstrumentKind.Histogram:
            case InstrumentKind.Gauge:
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    OutLn("    {");

                    OutLn($"     \"MetricName\": \"{metricMethod.MetricName.Replace("\\", "\\\\").Replace("\"", "\\\"")}\",");

                    if (!string.IsNullOrEmpty(metricMethod.Summary))
                    {
                        OutLn($"     \"MetricDescription\": \"{metricMethod.Summary.Replace("\\", "\\\\").Replace("\"", "\\\"")}\",");
                    }

                    Out($"     \"InstrumentName\": \"{metricMethod.Kind}\"");

                    if (metricMethod.Dimensions.Count > 0)
                    {
                        OutLn(",");

                        Out("     \"Dimensions\": {");

                        int k = 0;

                        foreach (var dimension in metricMethod.Dimensions)
                        {
                            OutLn();
                            if (metricMethod.DimensionsDescriptions.TryGetValue(dimension, out var description))
                            {
                                Out($"      \"{dimension}\": \"{description.Replace("\\", "\\\\").Replace("\"", "\\\"")}\"");
                            }
                            else
                            {
                                Out($"      \"{dimension}\": \"\"");
                            }

                            if (k < metricMethod.Dimensions.Count - 1)
                            {
                                Out(",");
                            }

                            k++;
                        }

                        OutLn();
                        Out("      }");
                        OutLn();
                    }
                    else
                    {
                        OutLn();
                    }

                    Out("    }");
                }
                catch (Exception e)
                {
                    // This should report diagnostic.
                    throw new InvalidOperationException($"An exception occurred during metric report generation {e.GetType()}:{e.Message}.");
                }

                break;
            case InstrumentKind.None:
            case InstrumentKind.CounterT:
            case InstrumentKind.HistogramT:
            default:
                // This should report diagnostic.
                throw new NotSupportedException($"Report for metric kind: '{metricMethod.Kind}' is not supported.");
        }
    }
}
