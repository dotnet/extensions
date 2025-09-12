// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.Gen.Shared;
using VerifyTests;
using VerifyXunit;
using Xunit;

namespace Microsoft.Gen.BuildMetadata.Test;

[Collection("BuildMetadataEmitterTests")]
public class GeneratorTests
{
    private readonly VerifySettings _verifySettings;

    public GeneratorTests()
    {
        _verifySettings = new VerifySettings();
        _verifySettings.UseDirectory("Verified");

        _verifySettings.ScrubLinesWithReplace(value =>
        {
            if (value.Contains(GeneratorUtilities.GeneratedCodeAttribute))
            {
                return value.Replace(GeneratorUtilities.CurrentVersion, "VERSION");
            }

            return value;
        });
    }

    [Theory]
    [CombinatorialData]
    public async Task BuildMetadataGenerator_ShouldGenerate([CombinatorialValues(true, false, null)] bool? isAzureDevOps)
    {
        var source = string.Empty; // Empty source, no attributes

        // Create test options based on the isAzureDevOps parameter
        var optionsProvider = CreateTestOptionsProvider(isAzureDevOps);

        var (d, sources) = await RunGenerator(source, optionsProvider);

        d.Should().BeEmpty();
        sources.Should().HaveCount(1);

        var settings = new VerifySettings(_verifySettings);
        settings.DisableRequireUniquePrefix();
        settings.UseParameters(isAzureDevOps);

        await Verifier.Verify(sources.Select(s => s.SourceText.ToString()), settings);
    }

    private static TestAnalyzerConfigOptionsProvider CreateTestOptionsProvider(bool? isAzureDevOps)
    {
        return new TestAnalyzerConfigOptionsProvider(new Dictionary<string, string?>
        {
            { "build_property.BuildMetadataAzureBuildId", "TEST_AZURE_BUILDID" },
            { "build_property.BuildMetadataAzureBuildNumber", "TEST_AZURE_BUILDNUMBER" },
            { "build_property.BuildMetadataAzureSourceBranchName", "TEST_AZURE_SOURCEBRANCHNAME" },
            { "build_property.BuildMetadataAzureSourceVersion", "TEST_AZURE_SOURCEVERSION" },
            { "build_property.BuildMetadataIsAzureDevOps", isAzureDevOps?.ToString() ?? "false" },
            { "build_property.BuildMetadataGitHubRunId", "TEST_GITHUB_RUNID" },
            { "build_property.BuildMetadataGitHubRunNumber", "TEST_GITHUB_RUNNUMBER" },
            { "build_property.BuildMetadataGitHubRefName", "TEST_GITHUB_REFNAME" },
            { "build_property.BuildMetadataGitHubSha", "TEST_GITHUB_SHA" }
        });
    }

    private static async Task<(IReadOnlyList<Diagnostic> diagnostics, IReadOnlyList<GeneratedSourceResult> sources)> RunGenerator(
        string source,
        TestAnalyzerConfigOptionsProvider optionsProvider)
    {
        // Create a test project and compilation
        var proj = RoslynTestUtils.CreateTestProject(Array.Empty<Assembly>());
        proj = proj.WithDocument("source.cs", source);
        proj.CommitChanges();

        var comp = await proj.GetCompilationAsync();

        // Create the generator driver with the options provider
        var driver = Microsoft.CodeAnalysis.CSharp.CSharpGeneratorDriver.Create(
            generators: new[] { new BuildMetadataGenerator().AsSourceGenerator() },
            optionsProvider: optionsProvider);

        var result = driver.RunGenerators(comp!);
        var runResult = result.GetRunResult();

        return (runResult.Results[0].Diagnostics, runResult.Results[0].GeneratedSources);
    }

    private sealed class TestAnalyzerConfigOptionsProvider : AnalyzerConfigOptionsProvider
    {
        private readonly TestAnalyzerConfigOptions _globalOptions;

        public TestAnalyzerConfigOptionsProvider(Dictionary<string, string?> globalOptions)
        {
            _globalOptions = new TestAnalyzerConfigOptions(globalOptions);
        }

        public override AnalyzerConfigOptions GlobalOptions => _globalOptions;

        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => _globalOptions;

        public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => _globalOptions;
    }

    private sealed class TestAnalyzerConfigOptions : AnalyzerConfigOptions
    {
        private readonly Dictionary<string, string?> _options;

        public TestAnalyzerConfigOptions(Dictionary<string, string?> options)
        {
            _options = options;
        }

        public override bool TryGetValue(string key, [NotNullWhen(true)] out string? value)
        {
            return _options.TryGetValue(key, out value);
        }

        public override IEnumerable<string> Keys => _options.Keys;
    }
}
