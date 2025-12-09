// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Shared.DiagnosticIds;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>
/// A chat client that reduces the size of a message list.
/// </summary>
[Experimental(DiagnosticIds.Experiments.ChatReduction, Message = DiagnosticIds.Experiments.ChatReductionMessage, UrlFormat = DiagnosticIds.UrlFormat)]
public sealed class ReducingChatClient : DelegatingChatClient
{
    private readonly IChatReducer _reducer;

    /// <summary>Initializes a new instance of the <see cref="ReducingChatClient"/> class.</summary>
    /// <param name="innerClient">The underlying <see cref="IChatClient"/>, or the next instance in a chain of clients.</param>
    /// <param name="reducer">The reducer to be used by this instance.</param>
    public ReducingChatClient(IChatClient innerClient, IChatReducer reducer)
        : base(innerClient)
    {
        _reducer = Throw.IfNull(reducer);
    }

    /// <inheritdoc />
    public override async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        messages = await _reducer.ReduceAsync(messages, cancellationToken).ConfigureAwait(false);

        return await base.GetResponseAsync(messages, options, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        messages = await _reducer.ReduceAsync(messages, cancellationToken).ConfigureAwait(false);

        await foreach (var update in base.GetStreamingResponseAsync(messages, options, cancellationToken).ConfigureAwait(false))
        {
            yield return update;
        }
    }
}
