// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;

namespace Microsoft.Extensions.AI.Templates.Tests;

public sealed class TemplateSandboxFixture
{
    public TemplateSandboxFixture()
    {
        var templateSandboxOutputRoot = Path.Combine(TestBase.TestProjectRoot, "TemplateSandbox", "output");
        Directory.Delete(templateSandboxOutputRoot, recursive: true);
    }
}
