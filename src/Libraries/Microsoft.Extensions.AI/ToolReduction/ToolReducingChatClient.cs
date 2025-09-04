// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>
/// A delegating chat client that applies a tool reduction strategy before invoking the inner client.
/// </summary>
/// <remarks>
/// Insert this into a pipeline (typically before function invocation middleware) to automatically
/// reduce the tool list carried on <see cref="ChatOptions"/> for each request.
/// </remarks>
[Experimental("MEAI001")]
public sealed class ToolReducingChatClient : DelegatingChatClient
{
    private readonly IToolReductionStrategy _strategy;

    /// <summary>
    /// Initializes a new instance of the <see cref="ToolReducingChatClient"/> class.
    /// </summary>
    /// <param name="innerClient">The inner client.</param>
    /// <param name="strategy">The tool reduction strategy to apply.</param>
    /// <exception cref="ArgumentNullException">Thrown if any argument is <see langword="null"/>.</exception>
    public ToolReducingChatClient(IChatClient innerClient, IToolReductionStrategy strategy)
        : base(innerClient)
    {
        _strategy = Throw.IfNull(strategy);
    }

    /// <inheritdoc />
    public override async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        options = await ApplyReductionAsync(messages, options, cancellationToken).ConfigureAwait(false);
        return await base.GetResponseAsync(messages, options, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        options = await ApplyReductionAsync(messages, options, cancellationToken).ConfigureAwait(false);

        await foreach (var update in base.GetStreamingResponseAsync(messages, options, cancellationToken).ConfigureAwait(false))
        {
            yield return update;
        }
    }

    private async Task<ChatOptions?> ApplyReductionAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options,
        CancellationToken cancellationToken)
    {
        // If there are no options or no tools, skip.
        if (options?.Tools is not { Count: > 1 })
        {
            return options;
        }

        IEnumerable<AITool> reduced;
        try
        {
            reduced = await _strategy.SelectToolsForRequestAsync(messages, options, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return options;
        }

        // If strategy returned the same list instance (or reference equality), assume no change.
        if (ReferenceEquals(reduced, options.Tools))
        {
            return options;
        }

        // Materialize and compare counts; if unchanged and tools have identical ordering and references, keep original.
        if (reduced is not IList<AITool> reducedList)
        {
            reducedList = reduced.ToList();
        }

        // Clone options to avoid mutating a possibly shared instance.
        var cloned = options.Clone();
        cloned.Tools = reducedList;
        return cloned;
    }
}
