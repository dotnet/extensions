// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Extensions.AI.Templates.Tests;

/// <summary>
/// Contains execution tests for the "AI Chat Web" template.
/// </summary>
/// <remarks>
/// In addition to validating that the templates build and restore correctly,
/// these tests are also responsible for template component governance reporting.
/// This is because the generated output is left on disk after tests complete,
/// most importantly the project.assets.json file that gets created during restore.
/// Therefore, it's *critical* that these tests remain in a working state,
/// as disabling them will also disable CG reporting.
/// </remarks>
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

    public static IEnumerable<object[]> GetBasicTemplateOptions()
        => GetFilteredTemplateOptions("--aspire", "false");

    public static IEnumerable<object[]> GetAspireTemplateOptions()
        => GetFilteredTemplateOptions("--aspire", "true");

    // Do not skip. See XML docs for this test class.
    [Theory]
    [MemberData(nameof(GetBasicTemplateOptions))]
    public async Task CreateRestoreAndBuild_BasicTemplate(params string[] args)
    {
        const string ProjectName = "BasicApp";
        var project = await Fixture.CreateProjectAsync(
            templateName: "aichatweb",
            projectName: ProjectName,
            args);

        await Fixture.RestoreProjectAsync(project);
        await Fixture.BuildProjectAsync(project);
    }

    // Do not skip. See XML docs for this test class.
    [Theory]
    [MemberData(nameof(GetAspireTemplateOptions))]
    public async Task CreateRestoreAndBuild_AspireTemplate(params string[] args)
    {
        const string ProjectName = "AspireApp";
        var project = await Fixture.CreateProjectAsync(
            templateName: "aichatweb",
            ProjectName,
            args);

        project.StartupProjectRelativePath = $"{ProjectName}.AppHost";

        await Fixture.RestoreProjectAsync(project);
        await Fixture.BuildProjectAsync(project);
    }

    private static readonly (string name, string[] values)[] _templateOptions = [
        ("--provider",          ["azureopenai", "githubmodels", "ollama", "openai"]),
        ("--vector-store",      ["azureaisearch", "local", "qdrant"]),
        ("--managed-identity",  ["true", "false"]),
        ("--aspire",            ["true", "false"]),
    ];

    private static IEnumerable<object[]> GetFilteredTemplateOptions(params string[] filter)
    {
        foreach (var options in GetAllPossibleOptions(_templateOptions))
        {
            if (!MatchesFilter())
            {
                continue;
            }

            if (HasOption("--managed-identity", "true"))
            {
                if (HasOption("--aspire", "true"))
                {
                    // The managed identity option is disabled for the Aspire template.
                    continue;
                }

                if (!HasOption("--vector-store", "azureaisearch") &&
                    !HasOption("--aspire", "false"))
                {
                    // Can only use managed identity when using Azure in the non-Aspire template.
                    continue;
                }
            }

            if (HasOption("--vector-store", "qdrant") &&
                HasOption("--aspire", "false"))
            {
                // Can't use Qdrant without Aspire.
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
