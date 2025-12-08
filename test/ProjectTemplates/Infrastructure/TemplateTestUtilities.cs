// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.TemplateEngine.Authoring.TemplateVerifier;

namespace Microsoft.Shared.ProjectTemplates.Tests;

public static class TemplateTestUtilities
{
    /// <summary>Create a sanitized and standardized project name from the supplied args</summary>
    /// <remarks>
    /// Framework name is shortened from "net10.0" to "net10", e.g.
    /// Boolean options explicitly set to "true" use the option name followed by '_true'.
    /// Boolean options explicitly set to "false" use the option name followed by '_false'.
    /// Options with names and values use only the option value.
    /// Options without values use the option name.
    /// Non-word characters are removed.
    /// Empty values are removed.
    /// </remarks>
    public static string GetProjectNameForArgs(string[] args, string? prefix = null)
    {
        IEnumerable<string> nameParts = args
            .Select(arg => Regex.Replace(arg, @"--[Ff]ramework=(net[0-9]+)\.0", "$1"))
            .Select(arg => Regex.Replace(arg, "--(.*?)=true", "$1_true"))
            .Select(arg => Regex.Replace(arg, "--(.*?)=false", "$1_false"))
            .Select(arg => Regex.Replace(arg, "--(.*?)=(.*)", "$2"))
            .Select(arg => Regex.Replace(arg, "--(.*)", "$1"))
            .Select(arg => Regex.Replace(arg, @"\W", ""))
            .Where(arg => !string.IsNullOrEmpty(arg));

        return (nameParts.Any(), prefix is not null) switch
        {
            (false, false) => "_defaults",
            (false, true) => $"{prefix}_defaults",
            (true, false) => string.Join('_', nameParts),
            _ => string.Join('_', nameParts.Prepend(prefix)),
        };
    }

    /// <summary>Gets all combinations of options provided for a project template.</summary>
    public static IEnumerable<string[]> GetPossibleOptions(ReadOnlyMemory<(string name, string[] values)> options)
    {
        if (options.Length == 0)
        {
            yield return [];
            yield break;
        }

        var firstOption = options.Span[0];

        foreach (var value in firstOption.values)
        {
            foreach (var otherOptions in GetPossibleOptions(options[1..]))
            {
                yield return [$"{firstOption.name}={value}", .. otherOptions];
            }
        }
    }

    /// <summary>Checks for a specific option/value pair</summary>
    public static bool HasOption(string[] args, string option, string value) =>
        args.Contains($"{option}={value}");

    /// <summary>Checks for a boolean option to be specified</summary>
    public static bool HasOption(string[] args, string option) =>
        args.Contains(option) || args.Contains($"{option}=true");

    public static ScrubbersDefinition AddSolutionFileGuidScrubber(this ScrubbersDefinition scrubbers) => scrubbers
        .AddScrubber(
            content => content.ScrubByRegex(pattern: @"\{.{36}\}", replacement: "{00000000-0000-0000-0000-000000000000}"),
            extension: "sln");

    public static ScrubbersDefinition AddUserSecretsScrubber(this ScrubbersDefinition scrubbers) => scrubbers
        .AddScrubber(
            content => content.ScrubByRegex(pattern: "<UserSecretsId>.{36}</UserSecretsId>", replacement: "<UserSecretsId>{00000000-0000-0000-0000-000000000000}</UserSecretsId>"),
            extension: "csproj");

    public static ScrubbersDefinition AddPackageReferenceScrubber(this ScrubbersDefinition scrubbers) => scrubbers
        .AddScrubber(
            content => content.ScrubByRegex(pattern: "<PackageReference Include=\"(.*)\" Version=\"(.*?)\" />", replacement: "<PackageReference Include=\"$1\" Version=\"{VERSION}\" />"),
            extension: "csproj");

    public static ScrubbersDefinition AddLocalhostPortScrubber(
        this ScrubbersDefinition scrubbers,
        (string pattern, string replacement)? https = null,
        (string pattern, string replacement)? http = null)
    {
        https ??= (@"\d{4,5}", "9995");
        http ??= (@"\d{4,5}", "9996");

        Action<StringBuilder> scrubPorts = content =>
        {
            content.ScrubByRegex($@"(https://localhost):({https.Value.pattern})", $"$1:{https.Value.replacement}");
            content.ScrubByRegex($@"(http://localhost):({http.Value.pattern})", $"$1:{http.Value.replacement}");
        };

        return scrubbers
            .AddScrubber(scrubPorts, extension: "md")
            .AddScrubber((path, content) =>
                {
                    if (Path.GetFileName(path).Equals("launchSettings.json", StringComparison.OrdinalIgnoreCase))
                    {
                        scrubPorts(content);
                    }
                });
    }
}
