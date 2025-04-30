// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.AI.Templates.Tests;

public sealed class TemplateExecutionTestConfiguration
{
    public required string TemplatePackageName { get; init; }

    public required string TestOutputFolderPrefix { get; init; }
}
