// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Shared.ProjectTemplates.Tests;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Agents.AI.ProjectTemplates.Tests;

/// <summary>
/// Contains execution tests for the "AI Agent Web API" template.
/// </summary>
/// <remarks>
/// In addition to validating that the templates build and restore correctly,
/// these tests are also responsible for template component governance reporting.
/// This is because the generated output is left on disk after tests complete,
/// most importantly the project.assets.json file that gets created during restore.
/// Therefore, it's *critical* that these tests remain in a working state,
/// as disabling them will also disable CG reporting.
/// </remarks>
public class WebApiAgentTemplateExecutionTests : TemplateExecutionTestBase<WebApiAgentTemplateExecutionTests>, ITemplateExecutionTestConfigurationProvider
{
    public WebApiAgentTemplateExecutionTests(TemplateExecutionTestFixture fixture, ITestOutputHelper outputHelper)
        : base(fixture, outputHelper)
    {
    }

    public static TemplateExecutionTestConfiguration Configuration { get; } = new()
    {
        TemplatePackageName = "Microsoft.Agents.AI.ProjectTemplates",
        TestOutputFolderPrefix = "WebApiAgent"
    };

    public static IEnumerable<object[]> GetTemplateOptions()
        => GetFilteredTemplateOptions();

    // Do not skip. See XML docs for this test class.
    [Theory]
    [MemberData(nameof(GetTemplateOptions))]
    public async Task CreateRestoreAndBuild(params string[] args)
    {
        const string ProjectName = "WebApiAgentApp";
        var project = await Fixture.CreateProjectAsync(
            templateName: "aiagent-webapi",
            projectName: ProjectName,
            args);

        await Fixture.RestoreProjectAsync(project);
        await Fixture.BuildProjectAsync(project);
    }

    private static readonly (string name, string[] values)[] _templateOptions = [
        ("--provider",         ["azureopenai", "githubmodels", "ollama", "openai"]),
        ("--managed-identity", ["true", "false"]),
    ];

    private static IEnumerable<object[]> GetFilteredTemplateOptions(params string[] filter)
    {
        foreach (var options in GetAllPossibleOptions(_templateOptions))
        {
            if (!MatchesFilter())
            {
                continue;
            }

            if (HasOption("--managed-identity", "true") && !HasOption("--provider", "azureopenai"))
            {
                // The managed identity option is only valid for Azure OpenAI.
                continue;
            }

            yield return options;

            bool MatchesFilter()
            {
                for (var i = 0; i < filter.Length; i += 2)
                {
                    if (!HasOption(filter[i], filter[i + 1]))
                    {
                        return false;
                    }
                }

                return true;
            }

            bool HasOption(string name, string value)
            {
                for (var i = 0; i < options.Length; i += 2)
                {
                    if (string.Equals(name, options[i], StringComparison.Ordinal) &&
                        string.Equals(value, options[i + 1], StringComparison.Ordinal))
                    {
                        return true;
                    }
                }

                return false;
            }
        }
    }

    private static IEnumerable<string[]> GetAllPossibleOptions(ReadOnlyMemory<(string name, string[] values)> options)
    {
        if (options.Length == 0)
        {
            yield return [];
            yield break;
        }

        var first = options.Span[0];
        foreach (var restSelection in GetAllPossibleOptions(options[1..]))
        {
            foreach (var value in first.values)
            {
                yield return [first.name, value, .. restSelection];
            }
        }
    }
}
