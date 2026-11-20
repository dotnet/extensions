// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Extensions.Telemetry.Test.BuildTransitive;

public class LoggingGeneratorTargetTests
{
    [Theory]
    [CombinatorialData]
    public async Task RemoveLoggingAnalyzers(
        [CombinatorialValues("net6.0", "net8.0")] string targetFramework,
        [CombinatorialValues("none", "before", "after")] string legacyTargetPosition,
        bool wpf,
        bool disableLoggingGenerator)
    {
        var repoRoot = GetRepoRoot();
        var tempDirectory = Path.Combine(Path.GetTempPath(), $"LoggingGeneratorTargetTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDirectory);

        try
        {
            var targetPath = Path.Combine(repoRoot, "src", "Libraries", "Microsoft.Extensions.Telemetry.Abstractions",
                "buildTransitive", targetFramework, "Microsoft.Extensions.Telemetry.Abstractions.targets");
            var import = $"""<Import Project="{SecurityElement.Escape(targetPath)}" />""";
            var hook = wpf ? "RemoveDuplicateAnalyzers" : "ResolveReferences";
            var legacyTargets = """
                <Target Name="_Microsoft_Extensions_Logging_AbstractionsRemoveAnalyzers"
                        Condition="'$(DisableMicrosoftExtensionsLoggingSourceGenerator)' == 'true'"
                        AfterTargets="ResolveReferences">
                  <ItemGroup>
                    <Analyzer Remove="@(Analyzer)" Condition="'%(Analyzer.NuGetPackageId)' == 'Microsoft.Extensions.Logging.Abstractions'" />
                  </ItemGroup>
                </Target>
                <Target Name="_Microsoft_Extensions_Logging_AbstractionsRemoveAnalyzersWPF"
                        Condition="'$(DisableMicrosoftExtensionsLoggingSourceGenerator)' == 'true'"
                        AfterTargets="RemoveDuplicateAnalyzers">
                  <ItemGroup>
                    <Analyzer Remove="@(Analyzer)" Condition="'%(Analyzer.NuGetPackageId)' == 'Microsoft.Extensions.Logging.Abstractions'" />
                  </ItemGroup>
                </Target>
                """;
            var project = $"""
                <Project>
                  <PropertyGroup>
                    <DisableMicrosoftExtensionsLoggingSourceGenerator>{disableLoggingGenerator}</DisableMicrosoftExtensionsLoggingSourceGenerator>
                  </PropertyGroup>
                  <ItemGroup>
                    <Analyzer Include="framework/Microsoft.Extensions.Logging.Generators.dll" />
                    <Analyzer Include="assembly/Logging.dll" AssemblyName="Microsoft.Extensions.Logging.Generators" />
                    <Analyzer Include="package/Logging.dll" NuGetPackageId="Microsoft.Extensions.Logging.Abstractions" />
                    <Analyzer Include="telemetry/Microsoft.Gen.Logging.dll" NuGetPackageId="Microsoft.Extensions.Telemetry.Abstractions" />
                    <Analyzer Include="unrelated/Analyzer.dll" AssemblyName="Unrelated.Analyzer" NuGetPackageId="Unrelated.Package" />
                    <Analyzer Include="unrelated/Microsoft.Extensions.Logging.Generators.Extra.dll" />
                  </ItemGroup>
                  {(legacyTargetPosition == "before" ? legacyTargets : string.Empty)}
                  {import}
                  {(legacyTargetPosition == "after" ? legacyTargets : string.Empty)}
                  <Target Name="{hook}" />
                  <Target Name="Test" DependsOnTargets="{hook}">
                    <WriteLinesToFile File="remaining.txt" Lines="@(Analyzer)" Overwrite="true" />
                  </Target>
                </Project>
                """;
            var projectPath = Path.Combine(tempDirectory, "test.proj");
            File.WriteAllText(projectPath, project);

            var dotnetFileName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "dotnet.exe" : "dotnet";
            var repoDotnetPath = Path.Combine(repoRoot, ".dotnet", dotnetFileName);
            var startInfo = new ProcessStartInfo(File.Exists(repoDotnetPath) ? repoDotnetPath : dotnetFileName)
            {
                WorkingDirectory = tempDirectory,
                Arguments = "msbuild test.proj -nologo -v:minimal -t:Test",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start dotnet.");
            var standardOutput = process.StandardOutput.ReadToEndAsync();
            var standardError = process.StandardError.ReadToEndAsync();
            if (!process.WaitForExit((int)TimeSpan.FromSeconds(30).TotalMilliseconds))
            {
                process.Kill();
                process.WaitForExit();
                throw new TimeoutException("Timed out while running dotnet msbuild.");
            }

            var output = await standardOutput;
            var error = await standardError;
            Assert.True(process.ExitCode == 0, output + error);

            var remaining = File.ReadAllLines(Path.Combine(tempDirectory, "remaining.txt"));
            Assert.Equal(disableLoggingGenerator ? 3 : 6, remaining.Length);
            Assert.Contains("telemetry/Microsoft.Gen.Logging.dll", remaining);
            Assert.Contains("unrelated/Analyzer.dll", remaining);
            Assert.Contains("unrelated/Microsoft.Extensions.Logging.Generators.Extra.dll", remaining);
            if (!disableLoggingGenerator)
            {
                Assert.Contains("framework/Microsoft.Extensions.Logging.Generators.dll", remaining);
                Assert.Contains("assembly/Logging.dll", remaining);
                Assert.Contains("package/Logging.dll", remaining);
            }
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    private static string GetRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "src", "Libraries", "Microsoft.Extensions.Telemetry.Abstractions",
                "buildTransitive", "net8.0", "Microsoft.Extensions.Telemetry.Abstractions.targets")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Failed to locate the repository root.");
    }
}
