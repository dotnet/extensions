// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace Microsoft.Extensions.Telemetry.Test.BuildTransitive;

public class RemoveLoggingGeneratorTargetTests
{
    [Theory]
    [InlineData("net6.0")]
    [InlineData("net8.0")]
    public async Task RemoveAnalyzers_LoggingAbstractionsTargetsImportedAfterOurs_RemovesAspNetCoreLoggingGenerator(string buildTransitiveFolder)
    {
        var result = await RunTargetAsync(buildTransitiveFolder);

        result.ExitCode.Should().Be(0, result.ToString());
        result.Output.Should().Contain("Analyzer: Microsoft.Gen.Logging", result.ToString());
        result.Output.Should().NotContain("Analyzer: Microsoft.Extensions.Logging.Generators", result.ToString());
    }

    private static async Task<CommandResult> RunTargetAsync(string buildTransitiveFolder)
    {
        var buildTransitivePath = Path.Combine(GetRepoRoot(), "src", "Libraries", "Microsoft.Extensions.Telemetry.Abstractions", "buildTransitive", buildTransitiveFolder);
        var tempDirectory = Path.Combine(Path.GetTempPath(), $"RemoveLoggingGeneratorTargetTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDirectory);

        try
        {
            var project = $"""
                <Project>
                  <Import Project="{EscapeXml(Path.Combine(buildTransitivePath, "Microsoft.Extensions.Telemetry.Abstractions.props"))}" />
                  <ItemGroup>
                    <Analyzer Include="Microsoft.AspNetCore.App.Ref/analyzers/dotnet/cs/Microsoft.Extensions.Logging.Generators.dll" AssemblyName="Microsoft.Extensions.Logging.Generators" />
                    <Analyzer Include="microsoft.extensions.telemetry.abstractions/analyzers/dotnet/cs/Microsoft.Gen.Logging.dll" NuGetPackageId="Microsoft.Extensions.Telemetry.Abstractions" />
                  </ItemGroup>
                  <Import Project="{EscapeXml(Path.Combine(buildTransitivePath, "Microsoft.Extensions.Telemetry.Abstractions.targets"))}" />
                  <!-- Microsoft.Extensions.Logging.Abstractions defines a target with this name. With package pruning, NuGet can import it after ours. -->
                  <Target Name="_Microsoft_Extensions_Logging_AbstractionsRemoveAnalyzers" />
                  <Target Name="ResolveReferences" />
                  <Target Name="PrintAnalyzers" DependsOnTargets="ResolveReferences">
                    <Message Importance="high" Text="Analyzer: %(Analyzer.Filename)" />
                  </Target>
                </Project>
                """;

            File.WriteAllText(Path.Combine(tempDirectory, "test.proj"), project);

            return await RunDotNetAsync(tempDirectory, "msbuild test.proj -nologo -v:minimal -t:PrintAnalyzers").ConfigureAwait(false);
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    private static async Task<CommandResult> RunDotNetAsync(string workingDirectory, string arguments)
    {
        var processStartInfo = new ProcessStartInfo(GetDotNetPath(), arguments)
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        using var process = Process.Start(processStartInfo) ?? throw new InvalidOperationException("Failed to start dotnet.");

        var standardOutputTask = process.StandardOutput.ReadToEndAsync();
        var standardErrorTask = process.StandardError.ReadToEndAsync();

        if (!process.WaitForExit((int)TimeSpan.FromSeconds(30).TotalMilliseconds))
        {
            process.Kill();
            process.WaitForExit();
        }

        var standardOutput = await standardOutputTask.ConfigureAwait(false);
        var standardError = await standardErrorTask.ConfigureAwait(false);

        return new CommandResult(process.ExitCode, standardOutput, standardError);
    }

    private static string GetDotNetPath()
    {
        var dotnetFileName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "dotnet.exe" : "dotnet";
        var repoDotnetPath = Path.Combine(GetRepoRoot(), ".dotnet", dotnetFileName);

        return File.Exists(repoDotnetPath) ? repoDotnetPath : dotnetFileName;
    }

    private static string GetRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (Directory.Exists(Path.Combine(directory.FullName, "src", "Libraries", "Microsoft.Extensions.Telemetry.Abstractions", "buildTransitive")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Failed to locate the repository root.");
    }

    private static string EscapeXml(string value)
    {
        return SecurityElement.Escape(value) ?? string.Empty;
    }

    private sealed record CommandResult(int ExitCode, string StandardOutput, string StandardError)
    {
        public string Output => StandardOutput + StandardError;

        public override string ToString()
        {
            return $"""
                Exit code: {ExitCode}
                Standard output:
                {StandardOutput}
                Standard error:
                {StandardError}
                """;
        }
    }
}
