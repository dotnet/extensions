// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.Telemetry.Enrichment;
using Microsoft.Extensions.Telemetry.Logging;
using Microsoft.Gen.Logging;
using Xunit;

namespace Microsoft.Extensions.ExtraAnalyzers.Test;

public class UsingToStringInLoggersTests
{
    public static IEnumerable<Assembly> References => new[]
    {
        Assembly.GetAssembly(typeof(LogMethodAttribute))!,
        Assembly.GetAssembly(typeof(IEnrichmentPropertyBag))!,
        Assembly.GetAssembly(typeof(Microsoft.Extensions.Logging.LogLevel))!,
    };

    [Fact]
    public async Task UsingToStringInLogger()
    {
        const string Source = @"
            using Microsoft.Extensions.Logging;

            namespace Example
            {
                internal static partial class Log
                {
                    [Microsoft.Extensions.Telemetry.Logging.LogMethod(1, LogLevel.Debug, ""Flargh! {arg}"")]
                    public static partial void Flargh(ILogger log, string arg);
                }

                public class TestContainer
                {
                    public object Item { get; private set; }
                    public TestContainer(object item)
                    {
                        Item = item;
                    }
                }

                public static class TestClass
                {
                    public static void LogTest(ILogger log, object something)
                    {
                        Log.Flargh(log, /*0+*/something.ToString()/*-0*/!);
                        Log.Flargh(log, /*1+*/something.GetType().ToString()/*-1*/!);
                        Log.Flargh(log, something.ToString()!.Substring(4));
                        var tc = new TestContainer(something);
                        Log.Flargh(log, /*2+*/tc.Item.ToString()/*-2*/!);
                    }
                }
            }";

        var generated = await RoslynTestUtils.RunGenerator(
            new LoggingGenerator(),
            References,
            new[] { Source }).ConfigureAwait(false);

        Assert.Equal(0, generated.diagnostics.Count);

        var fullDiags = await RoslynTestUtils.RunAnalyzer(
            new UsingToStringInLoggersAnalyzer(),
            References,
            new[] { Source, generated.generatedSources[0].SourceText.ToString() }).ConfigureAwait(false);

        var d = RoslynTestUtils.FilterDiagnostics(fullDiags, DiagDescriptors.UsingToStringInLoggers);

        Assert.Equal(3, d.Count);
        Source.AssertDiagnostic(0, DiagDescriptors.UsingToStringInLoggers, d[0]);
        Source.AssertDiagnostic(1, DiagDescriptors.UsingToStringInLoggers, d[1]);
        Source.AssertDiagnostic(2, DiagDescriptors.UsingToStringInLoggers, d[2]);
    }

    [Fact]
    public async Task NonLoggerAttribute()
    {
        const string Source = @"
                using Microsoft.Extensions.Logging;
                using System;

                namespace Example
                {
                    [AttributeUsage(AttributeTargets.Method)]
                    internal sealed class SimpleAttribute : Attribute
                    {
                        public string Text { get; }

                        public SimpleAttribute(string text) { Text = text; }
                    }

                    internal static class Log
                    {
                        [Simple(""This method is not a R9 LogMethod and should be ignored."")]
                        public static void Trace(ILogger log, string arg)
                        {
                            log.LogTrace(arg);
                        }
                    }

                    public static class TestClass
                    {
                        public static void LogTest(ILogger log, object something)
                        {
                            Log.Trace(log, something.ToString()!);
                        }
                    }
                }";

        var d = await RoslynTestUtils.RunAnalyzer(
            new UsingToStringInLoggersAnalyzer(),
            References,
            new[] { Source }).ConfigureAwait(false);

        Assert.Equal(0, d.Count);
    }
}
