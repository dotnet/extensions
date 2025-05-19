// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Microsoft.Extensions.AI.Templates.Tests;

internal static class WellKnownPaths
{
    public static readonly string RepoRoot;
    public static readonly string RepoDotNetExePath;
    public static readonly string ThisProjectRoot;

    public static readonly string TemplateFeedLocation;
    public static readonly string TemplateSandboxRoot;
    public static readonly string TemplateSandboxOutputRoot;
    public static readonly string TemplateInstallNuGetConfigPath;
    public static readonly string TemplateTestNuGetConfigPath;
    public static readonly string LocalShippingPackagesPath;
    public static readonly string NuGetPackagesPath;

    static WellKnownPaths()
    {
        RepoRoot = GetRepoRoot();
        RepoDotNetExePath = GetRepoDotNetExePath();
        ThisProjectRoot = ProjectRootHelper.GetThisProjectRoot();

        TemplateFeedLocation = Path.Combine(RepoRoot, "src", "ProjectTemplates");
        TemplateSandboxRoot = Path.Combine(ThisProjectRoot, "TemplateSandbox");
        TemplateSandboxOutputRoot = Path.Combine(TemplateSandboxRoot, "output");
        TemplateInstallNuGetConfigPath = Path.Combine(TemplateSandboxRoot, "nuget.template_install.config");
        TemplateTestNuGetConfigPath = Path.Combine(TemplateSandboxRoot, "nuget.template_test.config");

        const string BuildConfigurationFolder =
#if DEBUG
            "Debug";
#else
            "Release";
#endif
        LocalShippingPackagesPath = Path.Combine(RepoRoot, "artifacts", "packages", BuildConfigurationFolder, "Shipping");
        NuGetPackagesPath = Path.Combine(TemplateSandboxOutputRoot, "packages");
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
