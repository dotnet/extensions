// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Shared.ProjectTemplates.Tests;
using Xunit;
using Xunit.Abstractions;
using static Microsoft.Shared.ProjectTemplates.Tests.TemplateTestUtilities;

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
public class McpServerExecutionTests : TemplateExecutionTestBase<McpServerExecutionTests>, ITemplateExecutionTestConfigurationProvider
{
    public McpServerExecutionTests(TemplateExecutionTestFixture fixture, ITestOutputHelper outputHelper)
        : base(fixture, outputHelper)
    {
    }

    public static TemplateExecutionTestConfiguration Configuration { get; } = new()
    {
        TemplatePackageName = "Microsoft.Extensions.AI.Templates",
        TemplateName = "mcpserver"
    };

    public static IEnumerable<object[]> GetSupportedProjectConfigurations()
    {
        (string name, string[] values)[] allOptionValues = [
            ("--aot",               ["true", "false"]),
            ("--self-contained",    ["true", "false"]),
            ("--framework",         ["net8.0", "net9.0", "net10.0"])
        ];

        foreach (var args in GetPossibleOptions(allOptionValues))
        {
            // Managed Identity only applies when using an Azure service and not using aspire
            if (HasOption(args, "--managed-identity"))
            {
                if (HasOption(args, "--aspire"))
                {
                    continue;
                }

                if (!HasOption(args, "--provider", "azureopenai") &&
                    !HasOption(args, "--provider", "azureaifoundry") &&
                    !HasOption(args, "--vector-store", "azureaisearch"))
                {
                    continue;
                }
            }

            // Qdrant requires using Aspire orchestration
            if (HasOption(args, "--vector-store", "qdrant") && !HasOption(args, "--aspire"))
            {
                continue;
            }

            yield return args;
        }
    }

    // Do not skip. See XML docs for this test class.
    [Theory]
    [MemberData(nameof(GetSupportedProjectConfigurations))]
    public async Task TestAllSupportedConfigurations(params string[] args)
    {
        string projectName = GetProjectNameForArgs(args, prefix: "McpServer");
        await CreateRestoreAndBuild(projectName, args);
    }
}
