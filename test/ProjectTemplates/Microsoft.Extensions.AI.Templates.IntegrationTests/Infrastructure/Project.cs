// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;

namespace Microsoft.Extensions.AI.Templates.Tests;

public sealed class Project(string rootPath, string name)
{
    private string? _startupProjectRelativePath;
    private string? _startupProjectFullPath;

    public string RootPath => rootPath;

    public string Name => name;

    public string? StartupProjectRelativePath
    {
        get => _startupProjectRelativePath;
        set
        {
            if (value is null)
            {
                _startupProjectRelativePath = null;
                _startupProjectFullPath = null;
            }
            else if (!string.Equals(value, _startupProjectRelativePath, StringComparison.Ordinal))
            {
                _startupProjectRelativePath = value;
                _startupProjectFullPath = Path.Combine(rootPath, _startupProjectRelativePath);
            }
        }
    }

    public string StartupProjectFullPath => _startupProjectFullPath ?? rootPath;
}
