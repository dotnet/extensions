// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Extensions.AI.Templates.Tests;

public class BuildWebTemplateTests : TemplateTestBase<BuildWebTemplateTests>, ITemplateConfigurationProvider
{
    public BuildWebTemplateTests(Fixture fixture, ITestOutputHelper outputHelper)
        : base(fixture, outputHelper)
    {
    }

    public static TemplateConfiguration Configuration { get; } = new()
    {
        TemplatePackageName = "Microsoft.Extensions.AI.Templates",
        TestOutputFolderPrefix = "BuildWebTemplate"
    };

    [Fact]
    public async Task BuildBasicTemplate()
    {
        var fixture = GetFixture();
        var project = await fixture.CreateProjectAsync(
            templateName: "aichatweb",
            projectName: "BasicApp");

        await fixture.RestoreProjectAsync(project);
        await fixture.BuildProjectAsync(project);
    }

    [Fact]
    public async Task BuildAspireTemplate()
    {
        var fixture = GetFixture();
        var project = await fixture.CreateProjectAsync(
            templateName: "aichatweb",
            projectName: "AspireApp",
            args: ["--aspire"]);

        project.StartupProjectRelativePath = "AspireApp.AppHost";

        await fixture.RestoreProjectAsync(project);
        await fixture.BuildProjectAsync(project);
    }
}
