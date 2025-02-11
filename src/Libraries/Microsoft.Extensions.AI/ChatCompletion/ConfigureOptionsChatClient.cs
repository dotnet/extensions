// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>Represents a delegating chat client that configures a <see cref="ChatOptions"/> instance used by the remainder of the pipeline.</summary>
public sealed class ConfigureOptionsChatClient : DelegatingChatClient
{
    /// <summary>The callback delegate used to configure options.</summary>
    private readonly Action<ChatOptions> _configureOptions;

    /// <summary>Initializes a new instance of the <see cref="ConfigureOptionsChatClient"/> class with the specified <paramref name="configure"/> callback.</summary>
    /// <param name="innerClient">The inner client.</param>
    /// <param name="configure">
    /// The delegate to invoke to configure the <see cref="ChatOptions"/> instance. It is passed a clone of the caller-supplied <see cref="ChatOptions"/> instance
    /// (or a newly constructed instance if the caller-supplied instance is <see langword="null"/>).
    /// </param>
    /// <remarks>
    /// The <paramref name="configure"/> delegate is passed either a new instance of <see cref="ChatOptions"/> if
    /// the caller didn't supply a <see cref="ChatOptions"/> instance, or a clone (via <see cref="ChatOptions.Clone"/> of the caller-supplied
    /// instance if one was supplied.
    /// </remarks>
    public ConfigureOptionsChatClient(IChatClient innerClient, Action<ChatOptions> configure)
        : base(innerClient)
    {
        _configureOptions = Throw.IfNull(configure);
    }

    /// <inheritdoc/>
    public override async Task<ChatResponse> GetResponseAsync(IList<ChatMessage> chatMessages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        return await base.GetResponseAsync(chatMessages, Configure(options), cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IList<ChatMessage> chatMessages, ChatOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var update in base.GetStreamingResponseAsync(chatMessages, Configure(options), cancellationToken).ConfigureAwait(false))
        {
            yield return update;
        }
    }

    /// <summary>Creates and configures the <see cref="ChatOptions"/> to pass along to the inner client.</summary>
    private ChatOptions Configure(ChatOptions? options)
    {
        options = options?.Clone() ?? new();

        _configureOptions(options);

        return options;
    }
}
