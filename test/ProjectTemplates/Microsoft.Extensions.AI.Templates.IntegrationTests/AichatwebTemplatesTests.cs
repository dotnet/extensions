// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.AI.Templates.IntegrationTests;
using Microsoft.Extensions.AI.Templates.Tests;
using Microsoft.Extensions.Logging;
using Microsoft.TemplateEngine.Authoring.TemplateVerifier;
using Microsoft.TemplateEngine.TestHelper;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Extensions.AI.Templates.InegrationTests;

public class AichatwebTemplatesTests : TestBase
{
    // Keep the exclude patterns below in sync with those in Microsoft.Extensions.AI.Templates.csproj.
    private static readonly string[] _verificationExcludePatterns = [
        "**/bin/**",
        "**/obj/**",
        "**/node_modules/**",
        "**/*.user",
        "**/*.in",
        "**/*.out.js",
        "**/*.generated.css",
        "**/package-lock.json",
        "**/ingestioncache.db",
        "**/NuGet.config",
        "**/Directory.Build.targets",
    ];

    private readonly ILogger _log;

    public AichatwebTemplatesTests(ITestOutputHelper log)
    {
#pragma warning disable CA2000 // Dispose objects before losing scope
        _log = new XunitLoggerProvider(log).CreateLogger("TestRun");
#pragma warning restore CA2000 // Dispose objects before losing scope
    }

    [Fact]
    public async Task BasicTest()
    {
        string workingDir = TestUtils.CreateTemporaryFolder();
        string templateShortName = "aichatweb";

        // Get the template location
        string templateLocation = Path.Combine(TemplateFeedLocation, "Microsoft.Extensions.AI.Templates", "src", "ChatWithCustomData");

        var verificationExcludePatterns = Path.DirectorySeparatorChar is '/'
            ? _verificationExcludePatterns
            : _verificationExcludePatterns.Select(p => p.Replace('/', Path.DirectorySeparatorChar)).ToArray();

        TemplateVerifierOptions options = new TemplateVerifierOptions(templateName: templateShortName)
        {
            TemplatePath = templateLocation,
            SnapshotsDirectory = "Snapshots",
            OutputDirectory = workingDir,
            DoNotPrependCallerMethodNameToScenarioName = true,
            ScenarioName = "Basic",
            VerificationExcludePatterns = verificationExcludePatterns,
        }
        .WithCustomScrubbers(
            ScrubbersDefinition.Empty.AddScrubber((path, content) =>
            {
                string filePath = path.UnixifyDirSeparators();
                if (filePath.EndsWith("aichatweb/ChatWithCustomData.Web/ChatWithCustomData.Web.csproj") ||
                    filePath.EndsWith("aichatweb/aichatweb.csproj") ||
                    filePath.EndsWith("aichatweb/aichatweb.csproj.in"))
                {
                    content.ScrubByRegex("<UserSecretsId>(.*)<\\/UserSecretsId>", "<UserSecretsId>secret</UserSecretsId>");

                    // Scrub references to just-built packages and remove the suffix, if it exists.
                    // This allows the snapshots to remain the same regardless of where the repo is built (e.g., locally, public CI, internal CI).
                    var pattern = "<PackageReference\\s+Include=\\\"(Microsoft\\.Extensions\\.AI[^\"]*)\\\"\\s+Version=\\\"([^{{\\\"|\\-}}]*)[^\"]*\\\"\\s*\\/>";
                    var replacement = "<PackageReference Include=\"$1\" Version=\"$2\" />";
                    content.ScrubByRegex(pattern, replacement);
                }

                if (filePath.EndsWith("aichatweb/Properties/launchSettings.json"))
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
