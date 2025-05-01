// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Extensions.AI.Templates.Tests;

/// <summary>
/// Provides functionality scoped to the duration of all the tests in a single test class
/// extending <see cref="TemplateExecutionTestBase{TConfiguration}"/>.
/// </summary>
public abstract class TemplateExecutionTestClassFixtureBase : IAsyncLifetime
{
    private readonly TemplateExecutionTestConfiguration _configuration;
    private readonly string _templateTestOutputPath;
    private readonly string _customHivePath;
    private readonly MessageSinkTestOutputHelper _messageSinkTestOutputHelper;
    private ITestOutputHelper? _currentTestOutputHelper;

    /// <summary>
    /// Gets the current preferred output helper.
    /// If a test is underway, the output will be associated with that test.
    /// Otherwise, the output will appear as a diagnostic message via <see cref="IMessageSink"/>.
    /// </summary>
    private ITestOutputHelper OutputHelper => _currentTestOutputHelper ?? _messageSinkTestOutputHelper;

    protected TemplateExecutionTestClassFixtureBase(TemplateExecutionTestConfiguration configuration, IMessageSink messageSink)
    {
        _configuration = configuration;
        _messageSinkTestOutputHelper = new(messageSink);

        var outputFolderName = GetRandomizedFileName(prefix: _configuration.TestOutputFolderPrefix);
        _templateTestOutputPath = Path.Combine(WellKnownPaths.TemplateSandboxOutputRoot, outputFolderName);
        _customHivePath = Path.Combine(_templateTestOutputPath, "hive");
    }

    private static string GetRandomizedFileName(string prefix)
        => prefix + "_" + Guid.NewGuid().ToString("N").Substring(0, 10).ToLowerInvariant();

    public async Task InitializeAsync()
    {
        Directory.CreateDirectory(_templateTestOutputPath);

        await InstallTemplatesAsync();

        async Task InstallTemplatesAsync()
        {
            var installSandboxPath = Path.Combine(_templateTestOutputPath, "install");
            Directory.CreateDirectory(installSandboxPath);

            var installNuGetConfigPath = Path.Combine(installSandboxPath, "nuget.config");
            File.Copy(WellKnownPaths.TemplateInstallNuGetConfigPath, installNuGetConfigPath);

            var installResult = await new DotNetNewCommand("install", _configuration.TemplatePackageName)
                .WithWorkingDirectory(installSandboxPath)
                .WithEnvironmentVariable("LOCAL_SHIPPING_PATH", WellKnownPaths.LocalShippingPackagesPath)
                .WithEnvironmentVariable("NUGET_PACKAGES", WellKnownPaths.NuGetPackagesPath)
                .WithCustomHive(_customHivePath)
                .ExecuteAsync(OutputHelper);
            installResult.AssertSucceeded();
        }
    }

    public async Task<Project> CreateProjectAsync(string templateName, string projectName, params string[] args)
    {
        var outputFolderName = GetRandomizedFileName(projectName);
        var outputFolderPath = Path.Combine(_templateTestOutputPath, outputFolderName);

        ReadOnlySpan<string> dotNetNewCommandArgs = [
            templateName,
            "-o", outputFolderPath,
            "-n", projectName,
            "--no-update-check",
            .. args
        ];

        var newProjectResult = await new DotNetNewCommand(dotNetNewCommandArgs)
            .WithWorkingDirectory(_templateTestOutputPath)
            .WithCustomHive(_customHivePath)
            .ExecuteAsync(OutputHelper);
        newProjectResult.AssertSucceeded();

        var templateNuGetConfigPath = Path.Combine(outputFolderPath, "nuget.config");
        File.Copy(WellKnownPaths.TemplateTestNuGetConfigPath, templateNuGetConfigPath);

        return new Project(outputFolderPath, projectName);
    }

    public async Task RestoreProjectAsync(Project project)
    {
        var restoreResult = await new DotNetCommand("restore")
            .WithWorkingDirectory(project.StartupProjectFullPath)
            .WithEnvironmentVariable("LOCAL_SHIPPING_PATH", WellKnownPaths.LocalShippingPackagesPath)
            .WithEnvironmentVariable("NUGET_PACKAGES", WellKnownPaths.NuGetPackagesPath)
            .ExecuteAsync(OutputHelper);
        restoreResult.AssertSucceeded();
    }

    public async Task BuildProjectAsync(Project project)
    {
        var buildResult = await new DotNetCommand("build", "--no-restore")
            .WithWorkingDirectory(project.StartupProjectFullPath)
            .ExecuteAsync(OutputHelper);
        buildResult.AssertSucceeded();
    }

    public void SetCurrentTestOutputHelper(ITestOutputHelper? outputHelper)
    {
        if (_currentTestOutputHelper is not null && outputHelper is not null)
        {
            throw new InvalidOperationException(
                "Cannot set the template execution test output helper when one is already present. " +
                "This might be a sign that template execution tests are running in parallel, " +
                "which is not currently supported.");
        }

        _currentTestOutputHelper = outputHelper;
    }

    public Task DisposeAsync()
    {
        // Only here to implement IAsyncLifetime. Not currently used.
        return Task.CompletedTask;
    }
}
