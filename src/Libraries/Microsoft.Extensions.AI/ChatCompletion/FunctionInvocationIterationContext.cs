// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.DiagnosticIds;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>Provides context for an iteration within the function invocation loop.</summary>
/// <remarks>
/// This context is provided to the <see cref="FunctionInvokingChatClient.IterationCompleted"/>
/// callback after each iteration of the function invocation loop completes.
/// </remarks>
[Experimental(DiagnosticIds.Experiments.AIIterationCompleted, UrlFormat = DiagnosticIds.UrlFormat)]
public class FunctionInvocationIterationContext
{
    /// <summary>Gets or sets the current iteration number (0-based).</summary>
    /// <remarks>
    /// The initial request to the client that passes along the chat contents provided to the
    /// <see cref="FunctionInvokingChatClient"/> is iteration 0. If the client responds with
    /// a function call request that is processed, the next iteration is 1, and so on.
    /// </remarks>
    public int Iteration { get; set; }

    /// <summary>Gets or sets the aggregated usage details across all iterations so far.</summary>
    /// <remarks>
    /// This includes usage from all inner client calls and is updated after each iteration.
    /// May be <see langword="null"/> if the inner client doesn't provide usage information.
    /// </remarks>
    public UsageDetails? TotalUsage { get; set; }

    /// <summary>Gets or sets the messages accumulated during the function-calling loop.</summary>
    /// <remarks>
    /// This includes all messages from all iterations, including function call and result contents.
    /// </remarks>
    public IReadOnlyList<ChatMessage> Messages
    {
        get;
        set => field = Throw.IfNull(value);
    } = [];

    /// <summary>Gets or sets the response from the most recent inner client call.</summary>
    /// <remarks>
    /// This is the response that triggered the current iteration's function invocations.
    /// </remarks>
    public ChatResponse Response
    {
        get;
        set => field = Throw.IfNull(value);
    } = new([]);

    /// <summary>Gets or sets a value indicating whether to terminate the loop after this iteration.</summary>
    /// <remarks>
    /// <para>
    /// Setting this to <see langword="true"/> will cause the function invocation loop to exit
    /// after the current iteration completes. The function calls for this iteration will have
    /// already been processed before this callback is invoked.
    /// </para>
    /// <para>
    /// This is similar to setting <see cref="FunctionInvocationContext.Terminate"/> from within
    /// a function, but can be triggered based on external criteria like usage thresholds.
    /// </para>
    /// </remarks>
    public bool Terminate { get; set; }

    /// <summary>Gets or sets a value indicating whether the iteration is part of a streaming operation.</summary>
    public bool IsStreaming { get; set; }
}
