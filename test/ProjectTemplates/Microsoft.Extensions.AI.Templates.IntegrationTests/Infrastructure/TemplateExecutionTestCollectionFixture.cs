// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;

namespace Microsoft.Extensions.AI.Templates.Tests;

/// <summary>
/// Provides functionality scoped to the lifetime of all tests defined in
/// test classes extending <see cref="TemplateExecutionTestBase{TConfiguration}"/>.
/// </summary>
public sealed class TemplateExecutionTestCollectionFixture
{
    public TemplateExecutionTestCollectionFixture()
    {
        // Clear output from previous test run, if it exists.
        if (Directory.Exists(WellKnownPaths.TemplateSandboxOutputRoot))
        {
            Directory.Delete(WellKnownPaths.TemplateSandboxOutputRoot, recursive: true);
        }
    }
}
