// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;

namespace Microsoft.Shared.ProjectTemplates.Tests;

public sealed class TemplateExecutionTestConfiguration
{
    public required string TemplatePackageName { get; init; }
    public required string TemplateName { get; init; }

    private string TemplateSandboxRoot => Path.Combine(WellKnownPaths.ProjectTemplatesArtifactsRoot, TemplatePackageName, "Sandbox");
    public string TemplateSandboxPackages => Path.Combine(TemplateSandboxRoot, "packages");
    public string TemplateSandboxOutput => Path.Combine(TemplateSandboxRoot, TemplateName);
}
