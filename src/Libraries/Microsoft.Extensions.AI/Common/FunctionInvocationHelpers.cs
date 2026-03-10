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
}
