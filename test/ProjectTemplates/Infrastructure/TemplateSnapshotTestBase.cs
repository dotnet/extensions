// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Linq;
using Microsoft.TemplateEngine.Authoring.TemplateVerifier;

namespace Microsoft.Shared.ProjectTemplates.Tests;

public abstract class TemplateSnapshotTestBase
{
    protected virtual TemplateVerifierOptions PrepareSnapshotVerifier(
        string projectNamePrefix,
        string templatePackageName,
        string templateName,
        string[] templateArgs,
        string[]? verificationExcludePatterns = null)
    {
        // TemplateVerifierOptions.DoNotPrependTemplateNameToScenarioName results in a '_' prefix.
        // So skip setting a prefix here and let TemplateVerifier use the template name prefix.
        string scenarioName = TemplateTestUtilities.GetProjectNameForArgs(templateArgs);

        // Create a working directory using the same prefixing approach as the TemplateVerifier
        // to improve the debugging experience
        string workingDir = Path.Combine(WellKnownPaths.ProjectTemplatesArtifactsRoot, templatePackageName, "Snapshots", templateName, $"{templateName}.{scenarioName}");

        // Ensure the working directory is clean
        if (Directory.Exists(workingDir))
        {
            Directory.Delete(workingDir, recursive: true);
        }

        // Get the template location from the template package. Use a wildcard for the version number in the file name.
        string templateLocation = Path.Combine(WellKnownPaths.LocalShippingPackagesPath, $"{templatePackageName}.*.nupkg");

        string[]? excludePatterns = Path.DirectorySeparatorChar is '/'
            ? verificationExcludePatterns
            : verificationExcludePatterns?.Select(p => p.Replace('/', Path.DirectorySeparatorChar)).ToArray();

        return new TemplateVerifierOptions(templateName)
        {
            TemplatePath = templateLocation,
            TemplateSpecificArgs = templateArgs,
            SnapshotsDirectory = Path.Combine("Snapshots", templateName),
            OutputDirectory = workingDir,
            DoNotPrependCallerMethodNameToScenarioName = true,
            DoNotAppendTemplateArgsToScenarioName = true,
            ScenarioName = scenarioName,
            VerificationExcludePatterns = excludePatterns
        };
    }
}
