// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Telemetry.Logging;
using Microsoft.Gen.Shared;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Gen.ComplianceReports.Tests;

public class GeneratorTests
{
    private const string TestTaxonomy = @"
        using Microsoft.Extensions.Compliance.Classification;

        public sealed class C1Attribute : DataClassificationAttribute
        {
            public C1Attribute(new DataClassification(""TAX"", 1)) { }
        }

        public sealed class C2Attribute : DataClassificationAttribute
        {
            public C2Attribute(new DataClassification(""TAX"", 2)) { }
        }

        public sealed class C3Attribute : DataClassificationAttribute
        {
            public C3Attribute(new DataClassification(""TAX"", 4)) { }
        }

        public sealed class C4Attribute : DataClassificationAttribute
        {
            public C4Attribute(new DataClassification(""TAX"", 8)) { }
        }
    ";

    private readonly ITestOutputHelper _output;

    public GeneratorTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task TestAll()
    {
        foreach (var inputFile in Directory.GetFiles("TestClasses"))
        {
            var stem = Path.GetFileNameWithoutExtension(inputFile);
            var goldenReportFile = $"GoldenReports/{stem}.json";

            if (File.Exists(goldenReportFile))
            {
                var tmp = Path.GetTempFileName();
                var d = await RunGenerator(File.ReadAllText(inputFile), tmp);
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
                _ = await RunGenerator(File.ReadAllText(inputFile), goldenReportFile);
            }
        }

        static async Task<IReadOnlyList<Diagnostic>> RunGenerator(string code, string outputFile)
        {
            var (d, _) = await RoslynTestUtils.RunGenerator(
                new Generator(outputFile),
                    new[]
                    {
                        Assembly.GetAssembly(typeof(ILogger))!,
                        Assembly.GetAssembly(typeof(LogMethodAttribute))!,
                        Assembly.GetAssembly(typeof(Microsoft.Extensions.Compliance.Classification.DataClassification))!,
                    },
                    new[]
                    {
                        code,
                        TestTaxonomy,
                    },
                    new OptionsProvider()).ConfigureAwait(false);

            return d;
        }
    }

    [Fact]
    public async Task MissingDataClassificationSymbol()
    {
        const string Source = "class Nothing {}";

        var (d, _) = await RoslynTestUtils.RunGenerator(
                new Generator("Foo"),
                null,
                new[]
                {
                    Source,
                },
                new OptionsProvider()).ConfigureAwait(false);

        Assert.Equal(0, d.Count);
    }

    private sealed class Options : AnalyzerConfigOptions
    {
        public override bool TryGetValue(string key, out string value)
        {
            if (key == "build_property.GenerateComplianceReport")
            {
                value = bool.TrueString;
                return true;
            }

            value = null!;
            return false;
        }
    }

    private sealed class OptionsProvider : AnalyzerConfigOptionsProvider
    {
        public override AnalyzerConfigOptions GlobalOptions => new Options();
        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => throw new System.NotSupportedException();
        public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => throw new System.NotSupportedException();
    }
}
