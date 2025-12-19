// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.TemplateEngine.Authoring.TemplateVerifier;

namespace Microsoft.Shared.ProjectTemplates.Tests;

internal static class VerifyScrubbers
{
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
            content => content.ScrubByRegex(
                pattern: @"<PackageReference Include=""(.*)"" Version=""\d+(?:\.\d+)+(?:-[\w\.]+)?(?:\+[\w\.]+)?"" />",
                replacement: @"<PackageReference Include=""$1"" Version=""{VERSION}"" />"),
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
            .AddScrubber(scrubPorts, extension: "http")
            .AddScrubber((path, content) =>
                {
                    if (Path.GetFileName(path).Equals("launchSettings.json", StringComparison.OrdinalIgnoreCase))
                    {
                        scrubPorts(content);
                    }
                });
    }

    /// <summary>
    /// Removes content after "Details: ".
    /// </summary>
    internal static void ScrubDetails(this StringBuilder output)
    {
        output.ScrubByRegex("(Details: )([^\\r\\n]*)", $"Details: %DETAILS%");
    }

    /// <summary>
    /// Removes table header delimiter.
    /// </summary>
    internal static void ScrubTableHeaderDelimiter(this StringBuilder output)
    {
        output.ScrubByRegex("---[- ]*", "%TABLE HEADER DELIMITER%");
    }

    /// <summary>
    /// Replaces Windows newlines (CRLF) with Unix style newlines (LF).
    /// </summary>
    /// <param name="output"></param>
    internal static StringBuilder UnixifyNewlines(this StringBuilder output)
    {
        return output.Replace("\r\n", "\n");
    }

    /// <summary>
    /// Replaces Windows Directory separator char (\) with Unix Directory separator char (/).
    /// </summary>
    /// <param name="output"></param>
    internal static string UnixifyDirSeparators(this string output)
    {
        return output.Replace('\\', '/');
    }

    /// <summary>
    /// Replaces content matching <paramref name="pattern"/> with <paramref name="replacement"/>.
    /// </summary>
    internal static void ScrubByRegex(this StringBuilder output, string pattern, string replacement, RegexOptions regexOptions = RegexOptions.None)
    {
        string finalOutput = Regex.Replace(output.ToString(), pattern, replacement, regexOptions);
        output.Clear();
        output.Append(finalOutput);
    }

    /// <summary>
    /// Replaces content matching <paramref name="textToReplace"/> with <paramref name="replacement"/>.
    /// </summary>
    internal static void ScrubAndReplace(this StringBuilder output, string textToReplace, string replacement)
    {
        string finalOutput = output.ToString().Replace(textToReplace, replacement);
        output.Clear();
        output.Append(finalOutput);
    }
}
