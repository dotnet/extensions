// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Shared.ProjectTemplates.Tests;
using Microsoft.TemplateEngine.Authoring.TemplateVerifier;
using Microsoft.TemplateEngine.TestHelper;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Extensions.AI.Templates.Tests;

public class McpServerSnapshotTests : TemplateSnapshotTestBase
{
    private readonly ILogger _log;

    public McpServerSnapshotTests(ITestOutputHelper log)
    {
#pragma warning disable CA2000 // Dispose objects before losing scope
        _log = new XunitLoggerProvider(log).CreateLogger("TestRun");
#pragma warning restore CA2000 // Dispose objects before losing scope
    }

    [Theory]
    [InlineData /* Defaults: --self-contained=true --aot=false --framework=net10.0 */]
    [InlineData("--self-contained=false")]
    [InlineData("--aot=true")]
    [InlineData("--framework=net8.0")]
    public async Task TestSnapshots(params string[] templateArgs)
    {
        string projectNamePrefix = "McpServer";
        string templatePackageName = "Microsoft.Extensions.AI.Templates";
        string templateName = "mcpserver";

        TemplateVerifierOptions options = PrepareSnapshotVerifier(
            projectNamePrefix,
            templatePackageName,
            templateName,
            templateArgs)
        .WithCustomScrubbers(ScrubbersDefinition.Empty
            .AddPackageReferenceScrubber());

        VerificationEngine engine = new VerificationEngine(_log);
        await engine.Execute(options);
    }
}
