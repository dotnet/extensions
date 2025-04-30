// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.AI.Templates.Tests;

public sealed class Project(string rootPath, string name)
{
    public string RootPath => rootPath;

    public string Name => name;

    public string? StartupProjectRelativePath { get; set; }
}
