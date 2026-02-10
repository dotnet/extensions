// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Internal helper methods shared between <see cref="FunctionInvokingChatClient"/> and <see cref="FunctionInvokingRealtimeSession"/>.
/// </summary>
internal static class FunctionInvocationHelpers
{
    /// <summary>Gets a value indicating whether <see cref="Activity.Current"/> represents an "invoke_agent" span.</summary>
    internal static bool CurrentActivityIsInvokeAgent
    {
        get
        {
            string? name = Activity.Current?.DisplayName;
            return
                name?.StartsWith(OpenTelemetryConsts.GenAI.InvokeAgentName, StringComparison.Ordinal) is true &&
                (name.Length == OpenTelemetryConsts.GenAI.InvokeAgentName.Length || name[OpenTelemetryConsts.GenAI.InvokeAgentName.Length] == ' ');
        }
    }

    /// <summary>Gets the elapsed time since the given timestamp.</summary>
    internal static TimeSpan GetElapsedTime(long startingTimestamp) =>
#if NET
        Stopwatch.GetElapsedTime(startingTimestamp);
#else
        new((long)((Stopwatch.GetTimestamp() - startingTimestamp) * ((double)TimeSpan.TicksPerSecond / Stopwatch.Frequency)));
#endif

    /// <summary>Creates a mapping from tool names to the corresponding tools.</summary>
    /// <param name="toolLists">
    /// The lists of tools to combine into a single dictionary. Only <see cref="AIFunctionDeclaration"/>
    /// instances are included. Tools from later lists take precedence over tools from earlier lists
    /// if they have the same name.
    /// </param>
    /// <returns>A tuple containing the tool map and a flag indicating whether any tools require approval.</returns>
    internal static (Dictionary<string, AITool>? ToolMap, bool AnyRequireApproval) CreateToolsMap(params ReadOnlySpan<IList<AITool>?> toolLists)
    {
        Dictionary<string, AITool>? map = null;
        bool anyRequireApproval = false;

        foreach (var toolList in toolLists)
        {
            if (toolList?.Count is int count && count > 0)
            {
                for (int i = 0; i < count; i++)
                {
                    AITool tool = toolList[i];
                    if (tool is AIFunctionDeclaration)
                    {
                        anyRequireApproval |= tool.GetService<ApprovalRequiredAIFunction>() is not null;

                        // Later lists take precedence (options?.Tools overrides AdditionalTools)
                        map ??= new(StringComparer.Ordinal);
                        map[tool.Name] = tool;
                    }
                }
            }
        }

        return (map, anyRequireApproval);
    }
}
