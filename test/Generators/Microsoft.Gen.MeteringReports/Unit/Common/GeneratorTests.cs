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
using Microsoft.Extensions.Telemetry.Metering;
using Microsoft.Gen.Shared;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Gen.MeteringReports.Test;

public class GeneratorTests
{
    private readonly ITestOutputHelper _output;

    public GeneratorTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void GeneratorShouldNotDoAnythingIfGeneralExecutionContextDoesNotHaveClassDeclarationSyntaxReceiver()
    {
        var defaultGeneralExecutionContext = default(GeneratorExecutionContext);
        new MeteringReportsGenerator().Execute(defaultGeneralExecutionContext);

        Assert.Null(defaultGeneralExecutionContext.SyntaxReceiver);
    }

    [Fact]
    public async Task TestAll()
    {
        foreach (var inputFile in Directory.GetFiles("TestClasses"))
        {
            var stem = Path.GetFileNameWithoutExtension(inputFile);
            var goldenReportFile = $"GoldenReports/{stem}.json";

            var tmp = Path.Combine(Directory.GetCurrentDirectory(), "MeteringReport.json");

            if (File.Exists(goldenReportFile))
            {
                var d = await RunGenerator(File.ReadAllText(inputFile));
                Assert.Empty(d);

                var golden = File.ReadAllText(goldenReportFile);
                var generated = File.ReadAllText(tmp);

                if (golden != generated)
                {
                    _output.WriteLine($"MISMATCH: goldenReportFile {goldenReportFile}, tmp {tmp}");
                    _output.WriteLine("----");
                    _output.WriteLine("golden:");
                    _output.WriteLine(golden);
                    _output.WriteLine("----");
                    _output.WriteLine("generated:");
                    _output.WriteLine(generated);
                    _output.WriteLine("----");
                }

                Assert.Equal(golden, generated);
                File.Delete(tmp);
            }
            else
            {
                // generate the golden file if it doesn't already exist
                _output.WriteLine($"Generating golden report: {goldenReportFile}");
                _ = await RunGenerator(File.ReadAllText(inputFile));
                File.Copy(tmp, goldenReportFile);
            }
        }
    }

    private static async Task<IReadOnlyList<Diagnostic>> RunGenerator(
        string code,
        bool includeBaseReferences = true,
        bool includeMeterReferences = true,
        CancellationToken cancellationToken = default)
    {
        Assembly[]? refs = null;
        if (includeMeterReferences)
        {
            refs = new[]
            {
                Assembly.GetAssembly(typeof(Meter))!,
                Assembly.GetAssembly(typeof(CounterAttribute))!,
                Assembly.GetAssembly(typeof(HistogramAttribute))!,
                Assembly.GetAssembly(typeof(GaugeAttribute))!,
            };
        }

        var (d, _) = await RoslynTestUtils.RunGenerator(
            new MeteringReportsGenerator(),
            refs,
            new[] { code },
            new OptionsProvider(),
            includeBaseReferences: includeBaseReferences,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return d;
    }

    private sealed class Options : AnalyzerConfigOptions
    {
        public override bool TryGetValue(string key, out string value)
        {
            if (key == "build_property.GenerateMeteringReport")
            {
                value = bool.TrueString;
                return true;
            }

            if (key == "build_property.MeteringReportOutputPath")
            {
                value = Directory.GetCurrentDirectory();
                return true;
            }

            value = null!;
            return false;
        }
    }

    private sealed class OptionsProvider : AnalyzerConfigOptionsProvider
    {
        public override AnalyzerConfigOptions GlobalOptions => new Options();

        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => throw new NotSupportedException();
        public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => throw new NotSupportedException();
    }
}
