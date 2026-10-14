// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.Metrics;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.Compliance.Classification;
using Microsoft.Extensions.Diagnostics.Enrichment;
using Microsoft.Extensions.Logging;
using Microsoft.Gen.Logging.Parsing;
using Microsoft.Gen.Shared;
using Xunit;

namespace Microsoft.Gen.Logging.Test;

public partial class ParserTests
{
    [Theory]
    [InlineData("RateLimit(0)")]
    [InlineData("RateLimit(-1)")]
    [InlineData("RateLimit(16777216)")]
    [InlineData("RateLimit(1, WindowMilliseconds = 0)")]
    [InlineData("RateLimit(1, SegmentsPerWindow = 0)")]
    [InlineData("RateLimit(1, WindowMilliseconds = 10, SegmentsPerWindow = 11)")]
    public async Task RateLimit_InvalidValues(string attribute)
    {
        string source = @$"
            partial class C
            {{
                [LoggerMessage(0, LogLevel.Debug, ""M0"")]
                [/*0+*/{attribute}/*-0*/]
                static partial void M0(ILogger logger);
            }}";

        await RunGenerator(source, DiagDescriptors.InvalidRateLimit);
    }

    [Theory]
    [InlineData("RateLimit(1)")]
    [InlineData("RateLimit(16777215, WindowMilliseconds = 1, SegmentsPerWindow = 1)")]
    [InlineData("RateLimit(5, WindowMilliseconds = 60000, SegmentsPerWindow = 60000)")]
    public async Task RateLimit_ValidValues(string attribute)
    {
        string source = @$"
            partial class C
            {{
                [LoggerMessage(0, LogLevel.Debug, ""M0"")]
                [{attribute}]
                static partial void M0(ILogger logger);
            }}";

        await RunGenerator(source);
    }

    [Fact]
    public async Task RateLimit_TwoMeterParameters()
    {
        const string Source = @"
            partial class C
            {
                [LoggerMessage(0, LogLevel.Debug, ""M0"")]
                [RateLimit(1)]
                static partial void M0(ILogger logger, System.Diagnostics.Metrics.Meter meter1, System.Diagnostics.Metrics.Meter /*0+*/meter2/*-0*/);
            }";

        await RunGenerator(Source, DiagDescriptors.InvalidMeterParameter);
    }

    [Fact]
    public async Task RateLimit_MeterInTemplate()
    {
        const string Source = @"
            partial class C
            {
                [LoggerMessage(0, LogLevel.Debug, ""M0 {meter}"")]
                [RateLimit(1)]
                static partial void M0(ILogger logger, System.Diagnostics.Metrics.Meter /*0+*/meter/*-0*/);
            }";

        await RunGenerator(Source, DiagDescriptors.InvalidMeterParameter);
    }

    [Fact]
    public async Task RateLimit_MeterWithoutRateLimitIsOrdinaryParameter()
    {
        const string Source = @"
            partial class C
            {
                [LoggerMessage(0, LogLevel.Debug, ""M0 {meter}"")]
                static partial void M0(ILogger logger, System.Diagnostics.Metrics.Meter /*0+*/meter/*-0*/);
            }";

        await RunGenerator(Source, DiagDescriptors.DefaultToString);
    }

    [Fact]
    public async Task RateLimit_OnlyMeterAndLoggerIsEmptyLoggingMethod()
    {
        const string Source = @"
            partial class C
            {
                [LoggerMessage(Level = LogLevel.Debug)]
                [RateLimit(1)]
                static partial void /*0+*/M0/*-0*/(ILogger logger, System.Diagnostics.Metrics.Meter meter);
            }";

        await RunGenerator(Source, DiagDescriptors.EmptyLoggingMethod);
    }

    [Fact]
    public async Task RateLimit_GeneratedCode()
    {
        const string Source = @"
            namespace Test
            {
                using System.Diagnostics.Metrics;
                using Microsoft.Extensions.Logging;

                partial class C
                {
                    private int _M0RateLimiter;

                    [LoggerMessage(0, LogLevel.Error, ""M0"")]
                    [RateLimit(5, WindowMilliseconds = 2000, SegmentsPerWindow = 4)]
                    static partial void M0(ILogger logger);

                    [LoggerMessage(1, LogLevel.Error, ""M1"", SkipEnabledCheck = true, EventName = ""Event1"")]
                    [RateLimit(1)]
                    static partial void M1(ILogger logger, Meter? meter);

                    [LoggerMessage(2, LogLevel.Error, ""M2"")]
                    static partial void M2(ILogger logger);
                }
            }";

        string generated = await GenerateSourceAsync(Source);

        // The name of an existing member isn't reused.
        Assert.Contains(
            "private static readonly global::Microsoft.Extensions.Logging.LoggerMessageRateLimiter _M0RateLimiter_ = new(5, 2000, 4, nameof(M0));",
            generated,
            StringComparison.Ordinal);
        Assert.Contains("if (!_M0RateLimiter_.TryAcquire())", generated, StringComparison.Ordinal);

        Assert.Contains(
            "private static readonly global::Microsoft.Extensions.Logging.LoggerMessageRateLimiter _M1RateLimiter = new(1, 1000, 1, \"Event1\");",
            generated,
            StringComparison.Ordinal);
        Assert.Contains("if (!_M1RateLimiter.TryAcquire(meter))", generated, StringComparison.Ordinal);
        Assert.Contains("#pragma warning disable EXTEXP0003", generated, StringComparison.Ordinal);
        Assert.DoesNotContain("_M2RateLimiter", generated, StringComparison.Ordinal);

        // M0 is rate limited, so it checks IsEnabled even though it logs at the Error level.
        // M1 explicitly skips the check, and M2 keeps the default for the Error level.
        Assert.Equal(1, CountOccurrences(generated, ".IsEnabled("));
    }

    private static async Task<string> GenerateSourceAsync(string source)
    {
        var (diagnostics, sources) = await RoslynTestUtils.RunGenerator(
            new LoggingGenerator(),
            new[]
            {
                Assembly.GetAssembly(typeof(ILogger))!,
                Assembly.GetAssembly(typeof(LoggerMessageAttribute))!,
                Assembly.GetAssembly(typeof(IEnrichmentTagCollector))!,
                Assembly.GetAssembly(typeof(DataClassification))!,
                Assembly.GetAssembly(typeof(Meter))!,
            },
            new[] { source },
            Array.Empty<string>());

        Assert.Empty(diagnostics);
        return Assert.Single(sources).SourceText.ToString();
    }

    private static int CountOccurrences(string text, string value)
    {
        int count = 0;
        int index = text.IndexOf(value, StringComparison.Ordinal);
        while (index >= 0)
        {
            count++;
            index = text.IndexOf(value, index + value.Length, StringComparison.Ordinal);
        }

        return count;
    }
}
