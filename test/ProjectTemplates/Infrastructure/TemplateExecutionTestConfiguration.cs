// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;

namespace Microsoft.Shared.ProjectTemplates.Tests;

public sealed class TemplateExecutionTestConfiguration
{
    public required string TemplatePackageName { get; init; }
    public required string TemplateName { get; init; }

    public string TemplateSandboxOutput => Path.Combine(WellKnownPaths.TemplateSandboxOutputRoot, TemplateName);
}
