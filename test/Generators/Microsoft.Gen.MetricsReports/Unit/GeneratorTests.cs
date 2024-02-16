// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.Extensions.Diagnostics.Metrics;
using Microsoft.Gen.Shared;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Gen.MetricsReports.Test;

public class GeneratorTests(ITestOutputHelper output)
{
    [Fact]
    public void GeneratorShouldNotDoAnythingIfGeneralExecutionContextDoesNotHaveClassDeclarationSyntaxReceiver()
    {
        var defaultGeneralExecutionContext = default(GeneratorExecutionContext);
        new MetricsReportsGenerator().Execute(defaultGeneralExecutionContext);

        Assert.Null(defaultGeneralExecutionContext.SyntaxReceiver);
    }

    [Theory]
    [CombinatorialData]
    public async Task TestAll(bool useExplicitReportPath)
    {
        foreach (var inputFile in Directory.GetFiles("TestClasses"))
        {
            var stem = Path.GetFileNameWithoutExtension(inputFile);
            var goldenReportPath = Path.Combine("GoldenReports", Path.ChangeExtension(stem, ".json"));

            var generatedReportPath = Path.Combine(Directory.GetCurrentDirectory(), "MetricsReport.json");

            if (File.Exists(goldenReportPath))
            {
                var d = await RunGenerator(await File.ReadAllTextAsync(inputFile), useExplicitReportPath);
                Assert.Empty(d);

                var golden = await File.ReadAllTextAsync(goldenReportPath);
                var generated = await File.ReadAllTextAsync(generatedReportPath);

                if (golden != generated)
                {
                    output.WriteLine($"MISMATCH: goldenReportFile {goldenReportPath}, tmp {generatedReportPath}");
                    output.WriteLine("----");
                    output.WriteLine("golden:");
                    output.WriteLine(golden);
                    output.WriteLine("----");
                    output.WriteLine("generated:");
                    output.WriteLine(generated);
                    output.WriteLine("----");
                }

                File.Delete(generatedReportPath);
                Assert.Equal(golden, generated);
            }
            else
            {
                // generate the golden file if it doesn't already exist
                output.WriteLine($"Generating golden report: {goldenReportPath}");
                _ = await RunGenerator(await File.ReadAllTextAsync(inputFile), useExplicitReportPath);
                File.Copy(generatedReportPath, goldenReportPath);
            }
        }
    }

    private static async Task<IReadOnlyList<Diagnostic>> RunGenerator(
        string code,
        bool setReportPath,
        CancellationToken cancellationToken = default)
    {
        Assembly[] refs =
        [
            Assembly.GetAssembly(typeof(Meter))!,
            Assembly.GetAssembly(typeof(CounterAttribute))!,
            Assembly.GetAssembly(typeof(HistogramAttribute))!,
            Assembly.GetAssembly(typeof(GaugeAttribute))!
       ];

        var (d, _) = await RoslynTestUtils.RunGenerator(
            new MetricsReportsGenerator(),
            refs,
            new[] { code },
            new OptionsProvider(returnReportPath: setReportPath),
            includeBaseReferences: true,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return d;
    }

    private sealed class Options(bool returnReportPath) : AnalyzerConfigOptions
    {
        public override bool TryGetValue(string key, out string value)
        {
            if (key == "build_property.GenerateMetricsReport")
            {
                value = bool.TrueString;
                return true;
            }

            if (returnReportPath && key == "build_property.MetricsReportOutputPath")
            {
                value = Directory.GetCurrentDirectory();
                return true;
            }

            if (key == "build_property.outputpath")
            {
                value = Directory.GetCurrentDirectory();
                return true;
            }

            value = null!;
            return false;
        }
    }

    private sealed class OptionsProvider(bool returnReportPath) : AnalyzerConfigOptionsProvider
    {
        public override AnalyzerConfigOptions GlobalOptions => new Options(returnReportPath);

        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => throw new NotSupportedException();
        public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => throw new NotSupportedException();
    }
}
