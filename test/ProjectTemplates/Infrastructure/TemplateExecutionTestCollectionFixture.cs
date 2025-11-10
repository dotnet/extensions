// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;

namespace Microsoft.Shared.ProjectTemplates.Tests;

/// <summary>
/// Provides functionality scoped to the lifetime of all tests defined in
/// test classes extending <see cref="TemplateExecutionTestBase{TConfiguration}"/>.
/// </summary>
public sealed class TemplateExecutionTestCollectionFixture
{
    public TemplateExecutionTestCollectionFixture()
    {
        // Here, we clear execution test output from the previous test run, if it exists.
        //
        // It's critical that this clearing happens *before* the tests start, *not* after they complete.
        //
        // This is because:
        // 1. This enables debugging the previous test run by building/running generated projects manually.
        // 2. The existence of a project.assets.json file on disk is what allows template content to get discovered
        //    for component governance reporting.
        if (Directory.Exists(WellKnownPaths.TemplateSandboxOutputRoot))
        {
            Directory.Delete(WellKnownPaths.TemplateSandboxOutputRoot, recursive: true);
        }

        // Then we copy the template sandbox infrastructure to the output location for use during tests.
        Directory.CreateDirectory(WellKnownPaths.TemplateSandboxOutputRoot);

        foreach (var filePath in Directory.EnumerateFiles(WellKnownPaths.TemplateSandboxSource))
        {
            var fileName = Path.GetFileName(filePath);
            var destFilePath = Path.Combine(WellKnownPaths.TemplateSandboxOutputRoot, fileName);
            File.Copy(filePath, destFilePath);
        }
    }
}
