// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Extensions.AI.Templates.Tests;

public class AIChatWebExecutionTests : TemplateExecutionTestBase<AIChatWebExecutionTests>, ITemplateExecutionTestConfigurationProvider
{
    public AIChatWebExecutionTests(TemplateExecutionTestFixture fixture, ITestOutputHelper outputHelper)
        : base(fixture, outputHelper)
    {
    }

    public static TemplateExecutionTestConfiguration Configuration { get; } = new()
    {
        TemplatePackageName = "Microsoft.Extensions.AI.Templates",
        TestOutputFolderPrefix = "AIChatWeb"
    };

    [Theory]
    [InlineData("AzureApp",
        "--provider", "azureopenai",
        "--vector-store", "azureaisearch")]
    [InlineData("AzureAppManagedIdentity",
        "--provider", "azureopenai",
        "--vector-store", "azureaisearch",
        "--managed-identity")]
    [InlineData("GitHubModelsWithLocalStore",
        "--provider", "githubmodels",
        "--vector-store", "local")]
    [InlineData("OllamaWithLocalStore",
        "--provider", "ollama",
        "--vector-store", "local")]
    [InlineData("OpenAIWithLocalStore",
        "--provider", "openai",
        "--vector-store", "local")]
    public async Task CreateRestoreAndBuild_BasicTemplate(string projectName, params string[] args)
    {
        var project = await Fixture.CreateProjectAsync(
            templateName: "aichatweb",
            projectName,
            args);

        await Fixture.RestoreProjectAsync(project);
        await Fixture.BuildProjectAsync(project);
    }

    [Theory]
    [InlineData("AzureApp",
        "--provider", "azureopenai",
        "--vector-store", "azureaisearch")]
    [InlineData("GitHubModelsWithQdrant",
        "--provider", "githubmodels",
        "--vector-store", "qdrant")]
    [InlineData("OllamaWithLocalStore",
        "--provider", "ollama",
        "--vector-store", "local")]
    [InlineData("OpenAIWithLocalStore",
        "--provider", "ollama",
        "--vector-store", "local")]
    public async Task CreateRestoreAndBuild_AspireTemplate(string projectName, params string[] args)
    {
        var project = await Fixture.CreateProjectAsync(
            templateName: "aichatweb",
            projectName,
            args: ["--aspire", .. args]);

        project.StartupProjectRelativePath = $"{projectName}.AppHost";

        await Fixture.RestoreProjectAsync(project);
        await Fixture.BuildProjectAsync(project);
    }
}
