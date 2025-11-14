// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Shared.ProjectTemplates.Tests;
using Microsoft.TemplateEngine.Authoring.TemplateVerifier;
using Microsoft.TemplateEngine.TestHelper;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Agents.AI.ProjectTemplates.Tests;

public class WebApiAgentTemplateSnapshotTests
{
    // Keep the exclude patterns below in sync with those in Microsoft.Agents.AI.ProjectTemplates.csproj.
    private static readonly string[] _verificationExcludePatterns = [
        "**/bin/**",
        "**/obj/**",
        "**/.vs/**",
        "**/*.user",
        "**/*.in",
        "**/NuGet.config",
        "**/Directory.Build.targets",
        "**/Directory.Build.props"
    ];

    private readonly ILogger _log;

    public WebApiAgentTemplateSnapshotTests(ITestOutputHelper log)
    {
#pragma warning disable CA2000 // Dispose objects before losing scope
        _log = new XunitLoggerProvider(log).CreateLogger("TestRun");
#pragma warning restore CA2000 // Dispose objects before losing scope
    }

    [Fact]
    public async Task DefaultParameters()
    {
        await TestTemplateCoreAsync(scenarioName: nameof(DefaultParameters));
    }

    [Fact]
    public async Task GitHubModels()
    {
        await TestTemplateCoreAsync(scenarioName: nameof(GitHubModels), templateArgs: ["--provider", "githubmodels"]);
    }

    [Fact]
    public async Task OpenAI()
    {
        await TestTemplateCoreAsync(scenarioName: nameof(OpenAI), templateArgs: ["--provider", "openai"]);
    }

    [Fact]
    public async Task AzureOpenAI_ManagedIdentity()
    {
        await TestTemplateCoreAsync(scenarioName: nameof(AzureOpenAI_ManagedIdentity), templateArgs: ["--provider", "azureopenai"]);
    }

    [Fact]
    public async Task AzureOpenAI_ApiKey()
    {
        await TestTemplateCoreAsync(scenarioName: nameof(AzureOpenAI_ApiKey), templateArgs: ["--provider", "azureopenai", "--managed-identity", "false"]);
    }

    [Fact]
    public async Task Ollama()
    {
        await TestTemplateCoreAsync(scenarioName: nameof(Ollama), templateArgs: ["--provider", "ollama"]);
    }

    private async Task TestTemplateCoreAsync(string scenarioName, IEnumerable<string>? templateArgs = null)
    {
        string workingDir = TestUtils.CreateTemporaryFolder();
        string templateShortName = "aiagent-webapi";

        // Get the template location
        string templateLocation = Path.Combine(WellKnownPaths.TemplateFeedLocation, "Microsoft.Agents.AI.ProjectTemplates", "src", "WebApiAgent");

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
                if (filePath.EndsWith(".sln"))
                {
                    // Scrub .sln file GUIDs.
                    content.ScrubByRegex(pattern: @"\{.{36}\}", replacement: "{00000000-0000-0000-0000-000000000000}");
                }

                if (filePath.EndsWith(".csproj"))
                {
                    content.ScrubByRegex("<UserSecretsId>(.*)<\\/UserSecretsId>", "<UserSecretsId>secret</UserSecretsId>");

                    // Scrub references to just-built packages and remove the suffix, if it exists.
                    // This allows the snapshots to remain the same regardless of where the repo is built (e.g., locally, public CI, internal CI).
                    var pattern = @"(?<=<PackageReference\s+Include=""Microsoft\.(Agents|Extensions)\..*""\s+Version="")(\d+\.\d+\.\d+)(?:-[^""]*)?(?=""\s*/>)";
                    content.ScrubByRegex(pattern, replacement: "$2");
                }

                if (filePath.EndsWith("launchSettings.json") || filePath.EndsWith("README.md"))
                {
                    content.ScrubByRegex("(http(s?):\\/\\/localhost)\\:(\\d*)", "$1:9999");
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
