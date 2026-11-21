// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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

            var (exitCode, output) = await RunDotnetAsync(repoRoot, tempDirectory, "msbuild test.proj -nologo -v:minimal -t:Test");
            Assert.True(exitCode == 0, output);

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

    [Theory]
    [InlineData("Microsoft.NET.Sdk.Web", "10.11.0", "11.0.0", "net8.0", false)]
    [InlineData("Microsoft.NET.Sdk.Web", "11.0.0", "11.0.0", "net8.0", false)]
    [InlineData("Microsoft.NET.Sdk.Web", "10.11.0", "10.0.14", "net8.0", false)]
    [InlineData("Microsoft.NET.Sdk", "10.11.0", "11.0.0", "net8.0", false)]
    [InlineData("Microsoft.NET.Sdk.Web", "10.11.0", "11.0.0", "net6.0", false)]
    [InlineData("Microsoft.NET.Sdk.Web", "10.11.0", "11.0.0", "net8.0", true)]
    public async Task ConsumerBuildHasOneLoggingImplementation(
        string sdk, string telemetryVersion, string loggingVersion, string targetAsset, bool optOutDisabled)
    {
        var directory = CreateConsumerProject(sdk, telemetryVersion, loggingVersion, targetAsset, optOutDisabled);
        var (exitCode, output) = await BuildConsumerAsync(directory, optOutDisabled);
        Assert.True(exitCode == 0, output);

        var analyzers = File.ReadAllLines(Path.Combine(directory, "compiler-analyzers.txt")).Select(Path.GetFileName).ToArray();
        Assert.Contains("Microsoft.CodeAnalysis.CSharp.NetAnalyzers.dll", analyzers);
        Assert.Contains("Microsoft.Gen.Metrics.dll", analyzers);
        if (optOutDisabled)
        {
            var resolved = File.ReadAllLines(Path.Combine(directory, "resolved-analyzers.txt")).Select(Path.GetFileName).ToArray();
            Assert.Contains("Microsoft.Extensions.Logging.Generators.dll", resolved);
            Assert.Contains("Microsoft.Gen.Logging.dll", resolved);
            Assert.Single(analyzers, name => name == "Microsoft.Extensions.Logging.Generators.dll");
            Assert.DoesNotContain("Microsoft.Gen.Logging.dll", analyzers);
        }
        else
        {
            Assert.Contains("Microsoft.Gen.Logging.dll", analyzers);
            Assert.DoesNotContain("Microsoft.Extensions.Logging.Generators.dll", analyzers);
        }

        var implementations = Directory.GetFiles(Path.Combine(directory, "obj", "generated"), "*.cs", SearchOption.AllDirectories)
            .SelectMany(path => CSharpSyntaxTree.ParseText(File.ReadAllText(path)).GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>())
            .Where(type => type.Identifier.ValueText == "Log")
            .SelectMany(type => type.Members.OfType<MethodDeclarationSyntax>())
            .Where(method => method.Identifier.ValueText == "Started" && (method.Body is not null || method.ExpressionBody is not null));
        Assert.Single(implementations);
    }

    [Fact]
    public async Task ConsumerWithoutFixedTargetsReproducesDuplicateImplementation()
    {
        var directory = CreateConsumerProject("Microsoft.NET.Sdk.Web", "10.11.0", "11.0.0", targetAsset: null, optOutDisabled: false);
        var (exitCode, output) = await BuildConsumerAsync(directory, optOutDisabled: false);
        Assert.NotEqual(0, exitCode);
        Assert.Contains("CS0757", output);
        var analyzers = File.ReadAllLines(Path.Combine(directory, "compiler-analyzers.txt")).Select(Path.GetFileName).ToArray();
        Assert.Contains("Microsoft.Gen.Logging.dll", analyzers);
        Assert.Contains("Microsoft.Extensions.Logging.Generators.dll", analyzers);
    }

    private static string CreateConsumerProject(
        string sdk, string telemetryVersion, string loggingVersion, string? targetAsset, bool optOutDisabled)
    {
        var repoRoot = GetRepoRoot();

        // Keep compiler inputs, generated files and logs in build artifacts for diagnosing consumer failures.
        var directory = Path.Combine(repoRoot, "artifacts", "obj", "LoggingGeneratorConsumerTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var targetPath = targetAsset is null ? null : Path.Combine(repoRoot, "src", "Libraries", "Microsoft.Extensions.Telemetry.Abstractions",
            "buildTransitive", targetAsset, "Microsoft.Extensions.Telemetry.Abstractions.targets");
        var import = targetPath is null ? string.Empty : $"""<Import Project="{SecurityElement.Escape(targetPath)}" />""";

        // A false opt-out intentionally enables both generators. Isolate one standard generator after recording retention.
        var isolation = optOutDisabled ? """
            <ItemGroup>
              <_StandardLoggingGenerator Include="@(Analyzer)" Condition="'%(Analyzer.Filename)' == 'Microsoft.Extensions.Logging.Generators'" />
              <Analyzer Remove="@(Analyzer)" Condition="'%(Analyzer.Filename)' == 'Microsoft.Gen.Logging' Or
                '%(Analyzer.Filename)' == 'Microsoft.Extensions.Logging.Generators'" />
            </ItemGroup>
            <PropertyGroup>
              <_StandardLoggingGeneratorPaths>@(_StandardLoggingGenerator)</_StandardLoggingGeneratorPaths>
            </PropertyGroup>
            <ItemGroup>
              <Analyzer Include="$(_StandardLoggingGeneratorPaths.Split(';')[0])" />
            </ItemGroup>
            """ : string.Empty;
        var project = $"""
            <Project>
              <Import Project="Sdk.props" Sdk="{sdk}" />
              <PropertyGroup>
                <TargetFramework>net10.0</TargetFramework>
                <OutputType>Exe</OutputType>
                <Nullable>enable</Nullable>
                <ImplicitUsings>enable</ImplicitUsings>
                <EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
                <CompilerGeneratedFilesOutputPath>obj\generated</CompilerGeneratedFilesOutputPath>
              </PropertyGroup>
              <ItemGroup>
                <PackageReference Include="Microsoft.Extensions.Telemetry.Abstractions" Version="[{telemetryVersion}]" />
                <PackageReference Include="Microsoft.Extensions.Logging.Abstractions" Version="[{loggingVersion}]" />
              </ItemGroup>
              <Import Project="Sdk.targets" Sdk="{sdk}" />
              {import}
              <Target Name="RecordCompilerAnalyzers" BeforeTargets="CoreCompile" DependsOnTargets="ResolveReferences">
                <Message Importance="high" Text="Consumer SDK: $(NETCoreSdkVersion); targeting packs: @(ResolvedTargetingPack->'%(Path)')" />
                <WriteLinesToFile File="resolved-analyzers.txt" Lines="@(Analyzer)" Overwrite="true" />
                {isolation}
                <WriteLinesToFile File="compiler-analyzers.txt" Lines="@(Analyzer)" Overwrite="true" />
              </Target>
            </Project>
            """;
        File.WriteAllText(Path.Combine(directory, "consumer.csproj"), project);
        var program = $$"""
            using Microsoft.Extensions.Logging;

            {{(sdk == "Microsoft.NET.Sdk.Web" ? """
            var app = WebApplication.CreateBuilder(args).Build();
            Log.Started(app.Logger, "repro");
            """ : """
            Log.Started(Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance, "repro");
            """)}}

            internal static partial class Log
            {
                [LoggerMessage(Level = LogLevel.Information, Message = "Started {Name}")]
                public static partial void Started(ILogger logger, string name);
            }
            """;
        File.WriteAllText(Path.Combine(directory, "Program.cs"), program);
        return directory;
    }

    private static Task<(int exitCode, string output)> BuildConsumerAsync(string directory, bool optOutDisabled)
    {
        var arguments = "build consumer.csproj -nologo -v:minimal -p:ImportDirectoryBuildProps=false -p:ImportDirectoryBuildTargets=false " +
            "-p:ManagePackageVersionsCentrally=false" +
            (optOutDisabled ? " -p:DisableMicrosoftExtensionsLoggingSourceGenerator=false" : string.Empty);
        return RunDotnetAsync(GetRepoRoot(), directory, arguments);
    }

    private static async Task<(int exitCode, string output)> RunDotnetAsync(string repoRoot, string directory, string arguments)
    {
        var dotnetFileName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "dotnet.exe" : "dotnet";
        var repoDotnetPath = Path.Combine(repoRoot, ".dotnet", dotnetFileName);
        var startInfo = new ProcessStartInfo(File.Exists(repoDotnetPath) ? repoDotnetPath : dotnetFileName)
        {
            WorkingDirectory = directory,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start dotnet.");
        var standardOutput = process.StandardOutput.ReadToEndAsync();
        var standardError = process.StandardError.ReadToEndAsync();
        if (!process.WaitForExit((int)TimeSpan.FromMinutes(3).TotalMilliseconds))
        {
            process.Kill();
            process.WaitForExit();
            var timeoutOutput = await standardOutput + await standardError;
            File.WriteAllText(Path.Combine(directory, "build.log"), timeoutOutput);
            throw new TimeoutException($"Timed out while running dotnet {arguments}. {timeoutOutput}");
        }

        var output = await standardOutput + await standardError;
        File.WriteAllText(Path.Combine(directory, "build.log"), output);
        return (process.ExitCode, output);
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
