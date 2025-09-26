// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.Gen.BuildMetadata;

/// <summary>
/// Source generator that creates build metadata extensions from MSBuild properties.
/// Supports both Azure DevOps and GitHub Actions build environments.
/// </summary>
[Generator]
public class BuildMetadataGenerator : IIncrementalGenerator
{
    /// <inheritdoc/>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var buildPropertiesPipeline = context.AnalyzerConfigOptionsProvider.Select((provider, ct) =>
        {
            return CreateBuildMetadata(provider.GlobalOptions);
        });

        context.RegisterSourceOutput(buildPropertiesPipeline, Execute);
    }

    private static BuildMetadata CreateBuildMetadata(AnalyzerConfigOptions globalOptions)
    {
        // Azure DevOps properties
        var azureBuildId = globalOptions.GetProperty("BuildMetadataAzureBuildId");
        var azureBuildNumber = globalOptions.GetProperty("BuildMetadataAzureBuildNumber");
        var azureSourceBranchName = globalOptions.GetProperty("BuildMetadataAzureSourceBranchName");
        var azureSourceVersion = globalOptions.GetProperty("BuildMetadataAzureSourceVersion");
        var isAzureDevOps = globalOptions.GetBooleanProperty("BuildMetadataIsAzureDevOps");

        // GitHub Actions properties
        var gitHubRunId = globalOptions.GetProperty("BuildMetadataGitHubRunId");
        var gitHubRunNumber = globalOptions.GetProperty("BuildMetadataGitHubRunNumber");
        var gitHubRefName = globalOptions.GetProperty("BuildMetadataGitHubRefName");
        var gitHubSha = globalOptions.GetProperty("BuildMetadataGitHubSha");

        return new BuildMetadata(
            isAzureDevOps ? azureBuildId : gitHubRunId,
            isAzureDevOps ? azureBuildNumber : gitHubRunNumber,
            isAzureDevOps ? azureSourceBranchName : gitHubRefName,
            isAzureDevOps ? azureSourceVersion : gitHubSha);
    }

    private static void Execute(SourceProductionContext context, BuildMetadata buildMetadata)
    {
        var emitter = new Emitter(buildMetadata.BuildId, buildMetadata.BuildNumber, buildMetadata.SourceBranchName, buildMetadata.SourceVersion);
        var result = emitter.Emit(context.CancellationToken);
        context.AddSource("BuildMetadataExtensions.g.cs", SourceText.From(result, Encoding.UTF8));
    }

    private readonly record struct BuildMetadata(string? BuildId, string? BuildNumber, string? SourceBranchName, string? SourceVersion);
}
