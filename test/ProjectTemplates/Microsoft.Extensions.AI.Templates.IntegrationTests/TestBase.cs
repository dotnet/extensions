// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;

namespace Microsoft.Extensions.AI.Templates.IntegrationTests;

/// <summary>
/// The class contains the utils for unit and integration tests.
/// </summary>
public abstract class TestBase
{
    internal static string CodeBaseRoot { get; } = GetCodeBaseRoot();

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
}
