// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Internal helper methods shared between <see cref="FunctionInvokingChatClient"/> and <see cref="FunctionInvokingRealtimeClientSession"/>.
/// </summary>
internal static class FunctionInvocationHelpers
{
    /// <summary>Gets a value indicating whether <see cref="Activity.Current"/> represents an "invoke_agent" or "invoke_workflow" span.</summary>
    internal static bool CurrentActivityIsInvokeAgent
    {
        get
        {
            string? name = Activity.Current?.DisplayName;
            return
                IsActivityDisplayNameMatch(name, OpenTelemetryConsts.GenAI.InvokeAgentName) ||
                IsActivityDisplayNameMatch(name, OpenTelemetryConsts.GenAI.InvokeWorkflowName);
        }
    }

    /// <summary>
    /// Returns <see langword="true"/> if <paramref name="displayName"/> equals <paramref name="operationName"/>
    /// or starts with <paramref name="operationName"/> followed by a space (e.g. "invoke_agent my_agent").
    /// </summary>
    internal static bool IsActivityDisplayNameMatch(string? displayName, string operationName) =>
        displayName?.StartsWith(operationName, StringComparison.Ordinal) is true &&
        (displayName.Length == operationName.Length || displayName[operationName.Length] == ' ');

    /// <summary>Gets the elapsed time since the given timestamp.</summary>
    internal static TimeSpan GetElapsedTime(long startingTimestamp) =>
#if NET
        Stopwatch.GetElapsedTime(startingTimestamp);
#else
        new((long)((Stopwatch.GetTimestamp() - startingTimestamp) * ((double)TimeSpan.TicksPerSecond / Stopwatch.Frequency)));
#endif
}
