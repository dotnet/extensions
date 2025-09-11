// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.CodeAnalysis;
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

        Model.AzureBuildId = "AZURE_BUILDID";
        Model.AzureBuildNumber = "AZURE_BUILDNUMBER";
        Model.AzureSourceBranchName = "AZURE_SOURCEBRANCHNAME";
        Model.AzureSourceVersion = "AZURE_SOURCEVERSION";

        Model.GitHubRunId = "GITHUB_RUNID";
        Model.GitHubRunNumber = "GITHUB_RUNNUMBER";
        Model.GitHubRefName = "GITHUB_REFNAME";
        Model.GitHubSha = "GITHUB_SHA";

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
    public async Task BuildMetadataGenerator_ShouldGenerate(
        [CombinatorialValues(true, false, null)] bool? isAzureDevOps)
    {
        Model.IsAzureDevOps = isAzureDevOps != false;

        var source = string.Empty;
        var additionalRef = false;

        if (isAzureDevOps is null)
        {
            additionalRef = true;
            source = @"
            using Microsoft.Gen.BuildMetadata;
            [assembly: GenerateBuildMetadata(
                ""TEST_BUILDID"",
                ""TEST_BUILDNUMBER"",
                ""TEST_SOURCEBRANCHNAME"",
                ""TEST_SOURCEVERSION"")]";
        }

        var (d, sources) = await RunGenerator(source, additionalRef: additionalRef);

        d.Should().BeEmpty();
        sources.Should().HaveCount(1);

        var settings = new VerifySettings(_verifySettings);
        settings.DisableRequireUniquePrefix();
        settings.UseParameters(isAzureDevOps);

        await Verifier.Verify(sources.Select(s => s.SourceText.ToString()), settings);
    }

    private static async Task<(IReadOnlyList<Diagnostic> diagnostic, IReadOnlyList<GeneratedSourceResult> sources)> RunGenerator(string source, bool additionalRef = false)
    {
        Assembly[] refs = Array.Empty<Assembly>();

        if (additionalRef)
        {
            refs = new[] { Assembly.GetAssembly(typeof(GenerateBuildMetadataAttribute))! };
        }

        return await RoslynTestUtils.RunGenerator(new BuildMetadataGenerator(), refs, sources: [source]);
    }
}
