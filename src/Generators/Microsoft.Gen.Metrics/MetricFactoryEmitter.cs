// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Gen.Metrics.Model;
using Microsoft.Gen.Shared;

namespace Microsoft.Gen.Metrics;

// Stryker disable all

internal sealed class MetricFactoryEmitter : EmitterBase
{
    public string Emit(IReadOnlyList<MetricType> metricClasses, CancellationToken cancellationToken)
    {
        Dictionary<string, List<MetricType>> metricClassesDict = [];
        foreach (var cl in metricClasses)
        {
            if (!metricClassesDict.TryGetValue(cl.Namespace, out var list))
            {
                list = [];
                metricClassesDict.Add(cl.Namespace, list);
            }

            list.Add(cl);
        }

        foreach (var entry in metricClassesDict.OrderBy(static x => x.Key))
        {
            GenMetricFactoryByNamespace(entry.Key, entry.Value, cancellationToken);
        }

        return Capture();
    }

    private static string GetStringWithFirstCharLowercase(string str)
    {
        if (string.IsNullOrEmpty(str) ||
            char.IsLower(str[0]))
        {
            return str;
        }

        if (str.Length == 1)
        {
#pragma warning disable CA1308 // Normalize strings to uppercase
            return str.ToLowerInvariant();
#pragma warning restore CA1308 // Normalize strings to uppercase
        }

        return char.ToLowerInvariant(str[0]) + str.Substring(1);
    }

    private static string GetMetricDictionaryName(MetricMethod metricMethod)
        => $"_{GetStringWithFirstCharLowercase(metricMethod.MetricTypeName)}Instruments";

    private static string FormatBucketBoundaries(string type, IReadOnlyList<object> bucketBoundaries)
        => bucketBoundaries.Count == 0
            ? $"global::System.Array.Empty<{type}>()"
            : $"new {type}[] {{ {string.Join(", ", bucketBoundaries.Select(FormatBucketBoundary))} }}";

    // A literal of the boundary's own type that compiles back to exactly the same value. An int, short or byte constant
    // converts to the array's type implicitly.
    [SuppressMessage("Performance", "LA0002:Use 'Microsoft.Extensions.Text.NumericExtensions.ToInvariantString' for improved performance", Justification = "Can't use that in a generator")]
    private static string FormatBucketBoundary(object value) => value switch
    {
        double.PositiveInfinity => "double.PositiveInfinity",
        double.NegativeInfinity => "double.NegativeInfinity",
        double d => RoundTrip(d) + "D",
        float.PositiveInfinity => "float.PositiveInfinity",
        float.NegativeInfinity => "float.NegativeInfinity",
        float f => RoundTrip(f) + "F",
        long l => l.ToString(CultureInfo.InvariantCulture) + "L",
        int i => i.ToString(CultureInfo.InvariantCulture),
        short s => s.ToString(CultureInfo.InvariantCulture),
        byte b => b.ToString(CultureInfo.InvariantCulture),
        _ => throw new NotSupportedException($"Bucket boundary type '{value.GetType()}' is not supported"),
    };

    // "R" doesn't always round-trip on .NET Framework, where Visual Studio may run the generator; "G17" and "G9" always do.
    private static string RoundTrip(double value)
    {
        var text = value.ToString("R", CultureInfo.InvariantCulture);
        return double.Parse(text, CultureInfo.InvariantCulture).Equals(value) ? text : value.ToString("G17", CultureInfo.InvariantCulture);
    }

    private static string RoundTrip(float value)
    {
        var text = value.ToString("R", CultureInfo.InvariantCulture);
        return float.Parse(text, CultureInfo.InvariantCulture).Equals(value) ? text : value.ToString("G9", CultureInfo.InvariantCulture);
    }

    private void GenMetricFactoryByNamespace(string nspace, IEnumerable<MetricType> metricClasses, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(nspace))
        {
            OutLn($"namespace {nspace}");
            OutOpenBrace();
        }

        OutGeneratedCodeAttribute();
        OutLn("internal static partial class GeneratedInstrumentsFactory");
        OutOpenBrace();

        var first = true;
        foreach (var metricClass in metricClasses.OrderBy(static x => x.Name))
        {
            if (first)
            {
                first = false;
            }
            else
            {
                OutLn();
            }

            cancellationToken.ThrowIfCancellationRequested();
            GenMetricFactory(metricClass, nspace);
        }

        OutCloseBrace();

        if (!string.IsNullOrWhiteSpace(nspace))
        {
            OutCloseBrace();
        }

        OutLn();
    }

    private void GenMetricFactory(MetricType metricType, string nspace)
    {
        var nsprefix = string.IsNullOrWhiteSpace(nspace)
            ? string.Empty
            : $"global::{nspace}.";

        var count = metricType.Methods.Count;
        foreach (var metricMethod in metricType.Methods)
        {
            var meterParam = metricMethod.AllParameters[0];
            OutGeneratedCodeAttribute();
            OutLn($"private static global::System.Collections.Concurrent.ConcurrentDictionary<{meterParam.Type}, {nsprefix}{metricMethod.MetricTypeName}>");
            OutLn($"    {GetMetricDictionaryName(metricMethod)} = new();");
            if (--count != 0)
            {
                OutLn();
            }
        }

        foreach (var metricMethod in metricType.Methods)
        {
            GenMetricFactoryMethods(metricMethod, nspace);
        }
    }

    private void GenMetricFactoryMethods(MetricMethod metricMethod, string nspace)
    {
        var meterParam = metricMethod.AllParameters[0];
        string createMethodName = metricMethod.InstrumentKind switch
        {
            InstrumentKind.Counter => $"CreateCounter<{metricMethod.GenericType}>",
            InstrumentKind.CounterT => $"CreateCounter<{metricMethod.GenericType}>",
            InstrumentKind.Histogram => $"CreateHistogram<{metricMethod.GenericType}>",
            InstrumentKind.HistogramT => $"CreateHistogram<{metricMethod.GenericType}>",
            InstrumentKind.Gauge => $"CreateGauge<{metricMethod.GenericType}>",
            InstrumentKind.GaugeT => $"CreateGauge<{metricMethod.GenericType}>",
            _ => throw new NotSupportedException($"Metric type '{metricMethod.InstrumentKind}' is not supported to generate factory"),
        };

        var accessModifier = metricMethod.MetricTypeModifiers.Contains("public")
                ? "public"
                : "internal";

        var nsprefix = string.IsNullOrWhiteSpace(nspace)
            ? string.Empty
            : $"global::{nspace}.";

        OutLn();
        OutGeneratedCodeAttribute();
        OutLn($"{accessModifier} static {nsprefix}{metricMethod.MetricTypeName} Create{metricMethod.MetricTypeName}({meterParam.Type} {meterParam.Name})");
        OutOpenBrace();
        OutLn($"return {GetMetricDictionaryName(metricMethod)}.GetOrAdd({meterParam.Name}, static _meter =>");
        OutLn("    {");
        var unit = string.IsNullOrEmpty(metricMethod.MetricUnit) ? "null" : $"@\"{metricMethod.MetricUnit}\"";
        if (metricMethod.HistogramBucketBoundaries is { } bucketBoundaries)
        {
            var description = metricMethod.MetricDescription is { Length: > 0 } text ? SymbolDisplay.FormatLiteral(text, quote: true) : "null";
            var boundaries = FormatBucketBoundaries(metricMethod.GenericType, bucketBoundaries);
            OutLn($"        var instrument = _meter.{createMethodName}(@\"{metricMethod.MetricName}\", {unit}, {description}, tags: null,");
            OutLn($"            advice: new global::System.Diagnostics.Metrics.InstrumentAdvice<{metricMethod.GenericType}> {{ HistogramBucketBoundaries = {boundaries} }});");
        }
        else if (metricMethod.MetricDescription is { Length: > 0 } description)
        {
            OutLn($"        var instrument = _meter.{createMethodName}(@\"{metricMethod.MetricName}\", {unit}, {SymbolDisplay.FormatLiteral(description, quote: true)});");
        }
        else if (string.IsNullOrEmpty(metricMethod.MetricUnit))
        {
            OutLn($"        var instrument = _meter.{createMethodName}(@\"{metricMethod.MetricName}\");");
        }
        else
        {
            OutLn($"        var instrument = _meter.{createMethodName}(@\"{metricMethod.MetricName}\", @\"{metricMethod.MetricUnit}\");");
        }

        OutLn($"        return new {nsprefix}{metricMethod.MetricTypeName}(instrument);");
        OutLn("    });");
        OutCloseBrace();
    }
}
