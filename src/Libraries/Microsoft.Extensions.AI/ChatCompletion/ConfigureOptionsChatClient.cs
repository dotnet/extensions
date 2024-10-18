// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Shared.Diagnostics;

#pragma warning disable SA1629 // Documentation text should end with a period

namespace Microsoft.Extensions.AI;

/// <summary>A delegating chat client that updates or replaces the <see cref="ChatOptions"/> used by the remainder of the pipeline.</summary>
/// <remarks>
/// <para>
/// The configuration callback is invoked with the caller-supplied <see cref="ChatOptions"/> instance. To override the caller-supplied options
/// with a new instance, the callback may simply return that new instance, for example <c>_ => new ChatOptions() { MaxTokens = 1000 }</c>. To provide
/// a new instance only if the caller-supplied instance is `null`, the callback may conditionally return a new instance, for example
/// <c>options => options ?? new ChatOptions() { MaxTokens = 1000 }</c>. Any changes to the caller-provided options instance will persist on the
/// original instance, so the callback must take care to only do so when such mutations are acceptable, such as by cloning the original instance
/// and mutating the clone, for example:
/// <c>
/// options =>
/// {
///     var newOptions = options?.Clone() ?? new();
///     newOptions.MaxTokens = 1000;
///     return newOptions;
/// }
/// </c>
/// </para>
/// <para>
/// The provided implementation of <see cref="IChatClient"/> is thread-safe for concurrent use so long as the employed configuration
/// callback is also thread-safe for concurrent requests. If callers employ a shared options instance, care should be taken in the
/// configuration callback, as multiple calls to it may end up running in parallel with the same options instance.
/// </para>
/// </remarks>
public sealed class ConfigureOptionsChatClient : DelegatingChatClient
{
    /// <summary>The callback delegate used to configure options.</summary>
    private readonly Func<ChatOptions?, ChatOptions> _configureOptions;

    /// <summary>Initializes a new instance of the <see cref="ConfigureOptionsChatClient"/> class with the specified <paramref name="configureOptions"/> callback.</summary>
    /// <param name="innerClient">The inner client.</param>
    /// <param name="configureOptions">
    /// The delegate to invoke to configure the <see cref="ChatOptions"/> instance. It is passed the caller-supplied <see cref="ChatOptions"/>
    /// instance and should return the configured <see cref="ChatOptions"/> instance to use.
    /// </param>
    public ConfigureOptionsChatClient(IChatClient innerClient, Func<ChatOptions?, ChatOptions> configureOptions)
        : base(innerClient)
    {
        _configureOptions = Throw.IfNull(configureOptions);
    }

    /// <inheritdoc/>
    public override async Task<ChatCompletion> CompleteAsync(IList<ChatMessage> chatMessages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        return await base.CompleteAsync(chatMessages, _configureOptions(options), cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public override async IAsyncEnumerable<StreamingChatCompletionUpdate> CompleteStreamingAsync(
        IList<ChatMessage> chatMessages, ChatOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var update in base.CompleteStreamingAsync(chatMessages, _configureOptions(options), cancellationToken).ConfigureAwait(false))
        {
            yield return update;
        }
    }
}
