// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.TemplateEngine.Authoring.TemplateVerifier;
using Microsoft.TemplateEngine.TestHelper;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Extensions.AI.Templates.Tests;

public class McpServerSnapshotTests
{
    // Keep the exclude patterns below in sync with those in Microsoft.Extensions.AI.Templates.csproj.
    private static readonly string[] _verificationExcludePatterns = [
        "**/bin/**",
        "**/obj/**",
        "**/.vs/**",
        "**/*.sln",
        "**/*.in",
    ];

    private readonly ILogger _log;

    public McpServerSnapshotTests(ITestOutputHelper log)
    {
#pragma warning disable CA2000 // Dispose objects before losing scope
        _log = new XunitLoggerProvider(log).CreateLogger("TestRun");
#pragma warning restore CA2000 // Dispose objects before losing scope
    }

    [Fact]
    public async Task BasicTest()
    {
        await TestTemplateCoreAsync(scenarioName: "Basic");
    }

    [Fact]
    public async Task SelfContainedFalse()
    {
        await TestTemplateCoreAsync(scenarioName: "SelfContainedFalse", templateArgs: ["--self-contained", bool.FalseString]);
    }

    [Fact]
    public async Task AotTrue()
    {
        await TestTemplateCoreAsync(scenarioName: "AotTrue", templateArgs: ["--aot", bool.TrueString]);
    }

    [Fact]
    public async Task Net10()
    {
        await TestTemplateCoreAsync(scenarioName: "net10", templateArgs: ["--framework", "net10.0"]);
    }

    private async Task TestTemplateCoreAsync(string scenarioName, IEnumerable<string>? templateArgs = null)
    {
        string workingDir = TestUtils.CreateTemporaryFolder();
        string templateShortName = "mcpserver";

        // Get the template location
        string templateLocation = Path.Combine(WellKnownPaths.TemplateFeedLocation, "Microsoft.Extensions.AI.Templates", "src", "McpServer");

        var verificationExcludePatterns = Path.DirectorySeparatorChar is '/'
            ? _verificationExcludePatterns
            : _verificationExcludePatterns.Select(p => p.Replace('/', Path.DirectorySeparatorChar)).ToArray();

        TemplateVerifierOptions options = new TemplateVerifierOptions(templateName: templateShortName)
        {
            TemplatePath = templateLocation,
            TemplateSpecificArgs = templateArgs,
            SnapshotsDirectory = "Snapshots",
            OutputDirectory = workingDir,
            DoNotPrependCallerMethodNameToScenarioName = true,
            DoNotAppendTemplateArgsToScenarioName = true,
            ScenarioName = scenarioName,
            VerificationExcludePatterns = verificationExcludePatterns,
        }
        .WithCustomScrubbers(
            ScrubbersDefinition.Empty.AddScrubber((path, content) =>
            {
                string filePath = path.UnixifyDirSeparators();

                if (filePath.EndsWith(".csproj"))
                {
                    // Scrub references to just-built packages and remove the suffix, if it exists.
                    // This allows the snapshots to remain the same regardless of where the repo is built (e.g., locally, public CI, internal CI).
                    var pattern = @"(?<=<PackageReference\s+Include=""Microsoft\.Extensions\..*""\s+Version="")(\d+\.\d+\.\d+)(?:-[^""]*)?(?=""\s*/>)";
                    content.ScrubByRegex(pattern, replacement: "$1");
                }
            }));

        VerificationEngine engine = new VerificationEngine(_log);
        await engine.Execute(options);

#pragma warning disable CA1031 // Do not catch general exception types
        try
        {
            Directory.Delete(workingDir, recursive: true);
        }
        catch
        {
            /* don't care */
        }
#pragma warning restore CA1031 // Do not catch general exception types
    }
}
