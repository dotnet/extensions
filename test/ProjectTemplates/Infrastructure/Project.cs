// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;

namespace Microsoft.Shared.ProjectTemplates.Tests;

public sealed class Project(string rootPath, string name)
{
    public string RootPath => rootPath;

    public string Name => name;

    public string? StartupProjectRelativePath
    {
        get;
        set
        {
            if (value is null)
            {
                field = null;
                StartupProjectFullPath = null!;
            }
            else if (!string.Equals(value, field, StringComparison.Ordinal))
            {
                field = value;
                StartupProjectFullPath = Path.Combine(rootPath, field);
            }
        }
    }

    public string StartupProjectFullPath
    {
        get => field ?? rootPath;
        private set;
    }
}
