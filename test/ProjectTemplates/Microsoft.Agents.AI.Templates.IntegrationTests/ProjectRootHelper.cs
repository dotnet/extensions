// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;

namespace Microsoft.Agents.AI.Templates.Tests;

internal static class ProjectRootHelper
{
    public static string GetThisProjectRoot()
    {
        string? directory = Directory.GetCurrentDirectory();

        while (directory is not null)
        {
            string projectFile = Path.Combine(directory, "Microsoft.Agents.AI.Templates.Tests.csproj");
            if (File.Exists(projectFile))
            {
                return directory;
            }

            directory = Directory.GetParent(directory)?.FullName;
        }

        throw new System.InvalidOperationException("Failed to find project root");
    }
}
