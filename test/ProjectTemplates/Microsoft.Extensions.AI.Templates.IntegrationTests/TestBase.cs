// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Microsoft.Extensions.AI.Templates.Tests;

/// <summary>
/// The class contains the utils for unit and integration tests.
/// </summary>
public abstract class TestBase
{
    internal static string CodeBaseRoot { get; } = GetCodeBaseRoot();

    internal static string RepoDotNetExePath { get; } = GetRepoDotNetExePath();

    internal static string TestProjectRoot { get; } = GetTestProjectRoot();

    internal static string TemplateFeedLocation { get; } = Path.Combine(CodeBaseRoot, "src", "ProjectTemplates");

    private static string GetCodeBaseRoot()
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

    private static string GetTestProjectRoot([CallerFilePath] string callerFilePath = "")
    {
        if (Path.GetDirectoryName(callerFilePath) is not { Length: > 0 } testProjectRoot)
        {
            throw new InvalidOperationException("Could not determine the root of the test project.");
        }

        return testProjectRoot;
    }

    private static string GetRepoDotNetExePath()
    {
        var codeBaseRoot = CodeBaseRoot;
        var dotNetExeName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? "dotnet.exe"
            : "dotnet";

        var dotNetExePath = Path.Combine(codeBaseRoot, ".dotnet", dotNetExeName);

        if (!File.Exists(dotNetExePath))
        {
            throw new InvalidOperationException($"Expected to find '{dotNetExeName}' at '{dotNetExePath}', but it was not found.");
        }

        return dotNetExePath;
    }
}
