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

namespace Microsoft.Agents.AI.Templates.Tests;

public class AgentTemplateSnapshotTests
{
    // Exclude patterns will be defined here when actual template content is added.
    private static readonly string[] _verificationExcludePatterns = [
        "**/bin/**",
        "**/obj/**",
        "**/.vs/**",
        "**/node_modules/**",
        "**/*.user",
        "**/NuGet.config",
        "**/Directory.Build.targets",
        "**/Directory.Build.props",
    ];

    private readonly ILogger _log;

    public AgentTemplateSnapshotTests(ITestOutputHelper log)
    {
#pragma warning disable CA2000 // Dispose objects before losing scope
        _log = new XunitLoggerProvider(log).CreateLogger("TestRun");
#pragma warning restore CA2000 // Dispose objects before losing scope
    }

    // Placeholder test - actual template tests will be added when template content is implemented
    [Fact(Skip = "Template content not yet implemented")]
    public async Task BasicTest()
    {
        await TestTemplateCoreAsync(scenarioName: "Basic");
    }

    private async Task TestTemplateCoreAsync(string scenarioName, IEnumerable<string>? templateArgs = null)
    {
        string workingDir = TestUtils.CreateTemporaryFolder();
        string templateShortName = "agentapp"; // Placeholder - will be updated with actual template short name

        // Get the template location - will be updated when actual template is implemented
        string templateLocation = Path.Combine(WellKnownPaths.TemplateFeedLocation, "Microsoft.Agents.AI.Templates", "src", "PlaceholderTemplate");

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
              var pattern = @"(?<=<PackageReference\s+Include=""Microsoft\..*""\s+Version="")(\d+\.\d+\.\d+)(?:-[^""]*)?(?=""\s*/>)";
              content.ScrubByRegex(pattern, replacement: "$1");
          }

          if (filePath.EndsWith("launchSettings.json"))
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
