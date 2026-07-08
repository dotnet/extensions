// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Microsoft.Shared.ProjectTemplates.Tests;

internal static class WellKnownPaths
{
    private const string BuildConfigurationFolder =
#if DEBUG
        "Debug";
#else
        "Release";
#endif

    public static readonly string RepoRoot;
    public static readonly string RepoDotNetExePath;
    public static readonly string LocalShippingPackagesPath;
    public static readonly string ProjectTemplatesArtifactsRoot;

    // Execution Test Paths
    public static readonly string TemplateSandboxSource;
    public static readonly string TemplateTestNuGetConfigPath;

    static WellKnownPaths()
    {
        RepoRoot = GetRepoRoot();
        RepoDotNetExePath = GetRepoDotNetExePath();

        ProjectTemplatesArtifactsRoot = Path.Combine(RepoRoot, "artifacts", "ProjectTemplates");
        TemplateSandboxSource = Path.Combine(RepoRoot, "test", "ProjectTemplates", "Infrastructure", "TemplateSandbox");
        TemplateTestNuGetConfigPath = Path.Combine(TemplateSandboxSource, "nuget.config");

        LocalShippingPackagesPath = Path.Combine(RepoRoot, "artifacts", "packages", BuildConfigurationFolder, "Shipping");
    }

    private static string GetRepoRoot()
    {
        string? directory = AppContext.BaseDirectory;

        while (directory is not null)
        {
            var gitPath = Path.Combine(directory, ".git");
            if (Directory.Exists(gitPath) || File.Exists(gitPath))
            {
                // Found the repo root, which should either have a .git folder or, if the repo
                // is part of a Git worktree, a .git file.
                return directory;
            }

            directory = Directory.GetParent(directory)?.FullName;
        }

        throw new InvalidOperationException("Failed to establish root of the repository");
    }

    private static string GetRepoDotNetExePath()
    {
        var dotNetExeName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? "dotnet.exe"
            : "dotnet";

        var dotNetExePath = Path.Combine(RepoRoot, ".dotnet", dotNetExeName);

        if (!File.Exists(dotNetExePath))
        {
            throw new InvalidOperationException($"Expected to find '{dotNetExeName}' at '{dotNetExePath}', but it was not found.");
        }

        return dotNetExePath;
    }
}
