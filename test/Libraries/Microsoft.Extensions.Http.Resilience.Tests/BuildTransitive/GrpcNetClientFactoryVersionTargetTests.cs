// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace Microsoft.Extensions.Http.Resilience.Test.BuildTransitive;

public class GrpcNetClientFactoryVersionTargetTests
{
    private const string WarningMessage = "Grpc.Net.ClientFactory 2.63.0 or earlier could cause issues";

    public static TheoryData<string, string> CompatibleVersionSources => new()
    {
        {
            "PackageReference.Version",
            """
            <ItemGroup>
              <PackageReference Include="Grpc.Net.ClientFactory" Version="[2.80.0]" />
            </ItemGroup>
            """
        },
        {
            "PackageReference.VersionOverride",
            """
            <PropertyGroup>
              <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
            </PropertyGroup>
            <ItemGroup>
              <PackageReference Include="Grpc.Net.ClientFactory" VersionOverride="[2.80.0]" />
            </ItemGroup>
            """
        },
        {
            "PackageVersion.Version",
            """
            <PropertyGroup>
              <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
            </PropertyGroup>
            <ItemGroup>
              <PackageReference Include="Grpc.Net.ClientFactory" />
              <PackageVersion Include="Grpc.Net.ClientFactory" Version="[2.80.0]" />
            </ItemGroup>
            """
        },
        {
            "ReferencePath.NuGetPackageVersion",
            """
            <ItemGroup>
              <ReferencePath Include="Grpc.Net.ClientFactory.dll" NuGetPackageId="Grpc.Net.ClientFactory" NuGetPackageVersion="[2.80.0]" />
            </ItemGroup>
            """
        },
    };

    public static TheoryData<string, string> IncompatibleVersionSources => new()
    {
        {
            "PackageReference.Version",
            """
            <ItemGroup>
              <PackageReference Include="Grpc.Net.ClientFactory" Version="[2.63.0]" />
            </ItemGroup>
            """
        },
        {
            "PackageReference.VersionOverride",
            """
            <PropertyGroup>
              <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
            </PropertyGroup>
            <ItemGroup>
              <PackageReference Include="Grpc.Net.ClientFactory" VersionOverride="[2.63.0]" />
            </ItemGroup>
            """
        },
        {
            "PackageVersion.Version",
            """
            <PropertyGroup>
              <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
            </PropertyGroup>
            <ItemGroup>
              <PackageReference Include="Grpc.Net.ClientFactory" />
              <PackageVersion Include="Grpc.Net.ClientFactory" Version="[2.63.0]" />
            </ItemGroup>
            """
        },
        {
            "ReferencePath.NuGetPackageVersion",
            """
            <ItemGroup>
              <ReferencePath Include="Grpc.Net.ClientFactory.dll" NuGetPackageId="Grpc.Net.ClientFactory" NuGetPackageVersion="[2.63.0]" />
            </ItemGroup>
            """
        },
    };

    [Theory]
    [MemberData(nameof(CompatibleVersionSources))]
    public async Task CheckGrpcNetClientFactoryVersion_CompatibleBracketPinnedVersion_DoesNotWarnOrFail(
        string scenario,
        string projectItems)
    {
        var result = await RunTargetAsync(projectItems);

        result.ExitCode.Should().Be(0, result.ToString());
        result.Output.Should().NotContain("MSB4184", scenario);
        result.Output.Should().NotContain(WarningMessage, scenario);
    }

    [Theory]
    [MemberData(nameof(IncompatibleVersionSources))]
    public async Task CheckGrpcNetClientFactoryVersion_IncompatibleBracketPinnedVersion_Warns(
        string scenario,
        string projectItems)
    {
        var result = await RunTargetAsync(projectItems);

        result.ExitCode.Should().Be(0, result.ToString());
        result.Output.Should().NotContain("MSB4184", scenario);
        result.Output.Should().Contain(WarningMessage, scenario);
    }

    [Theory]
    [InlineData("(,2.80.0]")]
    [InlineData("[2.64.0-preview.1]")]
    public async Task CheckGrpcNetClientFactoryVersion_VersionWithoutComparableSystemVersion_DoesNotFail(string version)
    {
        var result = await RunTargetAsync($"""
            <ItemGroup>
              <PackageReference Include="Grpc.Net.ClientFactory" Version="{version}" />
            </ItemGroup>
            """);

        result.ExitCode.Should().Be(0, result.ToString());
        result.Output.Should().NotContain("MSB4184");
        result.Output.Should().NotContain(WarningMessage);
    }

    private static async Task<CommandResult> RunTargetAsync(string projectItems)
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), $"GrpcNetClientFactoryTargetTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDirectory);

        try
        {
            var projectPath = Path.Combine(tempDirectory, "test.proj");
            var project = $"""
                <Project>
                  <Import Project="{EscapeXml(GetTargetPath())}" />
                {projectItems}
                </Project>
                """;

            File.WriteAllText(projectPath, project);

            return await RunDotNetAsync(tempDirectory, "msbuild", projectPath, "-nologo", "-v:minimal", "-t:_CheckGrpcNetClientFactoryVersion").ConfigureAwait(false);
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    private static async Task<CommandResult> RunDotNetAsync(string workingDirectory, params string[] arguments)
    {
        var processStartInfo = new ProcessStartInfo(GetDotNetPath())
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            Arguments = CreateArguments(arguments),
        };

        using var process = Process.Start(processStartInfo) ?? throw new InvalidOperationException("Failed to start dotnet.");

        var standardOutputTask = process.StandardOutput.ReadToEndAsync();
        var standardErrorTask = process.StandardError.ReadToEndAsync();

        if (!process.WaitForExit((int)TimeSpan.FromSeconds(30).TotalMilliseconds))
        {
            process.Kill();
            throw new TimeoutException("Timed out while running dotnet msbuild.");
        }

        var standardOutput = await standardOutputTask.ConfigureAwait(false);
        var standardError = await standardErrorTask.ConfigureAwait(false);

        return new CommandResult(process.ExitCode, standardOutput, standardError);
    }

    private static string CreateArguments(params string[] arguments)
    {
        return string.Join(" ", Array.ConvertAll(arguments, QuoteArgument));
    }

    private static string QuoteArgument(string argument)
    {
        var quoted = new StringBuilder();
        quoted.Append('"');

        var backslashCount = 0;
        foreach (var character in argument)
        {
            if (character == '\\')
            {
                backslashCount++;
            }
            else if (character == '"')
            {
                quoted.Append('\\', (backslashCount * 2) + 1);
                quoted.Append('"');
                backslashCount = 0;
            }
            else
            {
                quoted.Append('\\', backslashCount);
                quoted.Append(character);
                backslashCount = 0;
            }
        }

        quoted.Append('\\', backslashCount * 2);
        quoted.Append('"');

        return quoted.ToString();
    }

    private static string GetTargetPath()
    {
        var repoRoot = GetRepoRoot();
        return Path.Combine(
            repoRoot,
            "src",
            "Libraries",
            "Microsoft.Extensions.Http.Resilience",
            "buildTransitive",
            "Microsoft.Extensions.Http.Resilience.targets");
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
            var targetPath = Path.Combine(
                directory.FullName,
                "src",
                "Libraries",
                "Microsoft.Extensions.Http.Resilience",
                "buildTransitive",
                "Microsoft.Extensions.Http.Resilience.targets");

            if (File.Exists(targetPath))
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
