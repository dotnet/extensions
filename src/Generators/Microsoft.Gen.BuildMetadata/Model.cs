// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

#pragma warning disable RS1035 // Do not use APIs banned for analyzers
#pragma warning disable S2223 // Non-constant static fields should not be visible - internal in order to be able to override this for tests

namespace Microsoft.Gen.BuildMetadata;

internal static class Model
{
    // Azure DevOps environment variables: https://learn.microsoft.com/azure/devops/pipelines/build/variables#build-variables-devops-services
    internal static string? AzureBuildId = Environment.GetEnvironmentVariable("Build_BuildId");
    internal static string? AzureBuildNumber = Environment.GetEnvironmentVariable("Build_BuildNumber");
    internal static string? AzureSourceBranchName = Environment.GetEnvironmentVariable("Build_SourceBranchName");
    internal static string? AzureSourceVersion = Environment.GetEnvironmentVariable("Build_SourceVersion");

    // GitHub Actions environment variables: https://docs.github.com/en/actions/learn-github-actions/variables#default-environment-variables
    internal static string? GitHubRunId = Environment.GetEnvironmentVariable("GITHUB_RUN_ID");
    internal static string? GitHubRunNumber = Environment.GetEnvironmentVariable("GITHUB_RUN_NUMBER");
    internal static string? GitHubRefName = Environment.GetEnvironmentVariable("GITHUB_REF_NAME");
    internal static string? GitHubSha = Environment.GetEnvironmentVariable("GITHUB_SHA");

    internal static bool IsAzureDevOps = string.Equals(Environment.GetEnvironmentVariable("TF_BUILD"), "true", StringComparison.OrdinalIgnoreCase);

    public static string? BuildId => IsAzureDevOps ? AzureBuildId : GitHubRunId;
    public static string? BuildNumber => IsAzureDevOps ? AzureBuildNumber : GitHubRunNumber;
    public static string? SourceBranchName => IsAzureDevOps ? AzureSourceBranchName : GitHubRefName;
    public static string? SourceVersion => IsAzureDevOps ? AzureSourceVersion : GitHubSha;
}
