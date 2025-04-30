// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;

namespace Microsoft.Extensions.AI.Templates.Tests;

public sealed class TemplateExecutionTestCollectionFixture
{
    public TemplateExecutionTestCollectionFixture()
    {
        Directory.Delete(WellKnownPaths.TemplateSandboxOutputRoot, recursive: true);
    }
}
