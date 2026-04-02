// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Shared.DiagnosticIds;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>Represents a delegating hosted conversation client that configures a <see cref="HostedConversationClientOptions"/> instance used by the remainder of the pipeline.</summary>
[Experimental(DiagnosticIds.Experiments.AIHostedConversation, UrlFormat = DiagnosticIds.UrlFormat)]
public sealed class ConfigureOptionsHostedConversationClient : DelegatingHostedConversationClient
{
    /// <summary>The callback delegate used to configure options.</summary>
    private readonly Action<HostedConversationClientOptions> _configureOptions;

    /// <summary>Initializes a new instance of the <see cref="ConfigureOptionsHostedConversationClient"/> class with the specified <paramref name="configure"/> callback.</summary>
    /// <param name="innerClient">The inner client.</param>
    /// <param name="configure">
    /// The delegate to invoke to configure the <see cref="HostedConversationClientOptions"/> instance. It is passed a clone of the caller-supplied <see cref="HostedConversationClientOptions"/> instance
    /// (or a newly constructed instance if the caller-supplied instance is <see langword="null"/>).
    /// </param>
    /// <remarks>
    /// The <paramref name="configure"/> delegate is passed either a new instance of <see cref="HostedConversationClientOptions"/> if
    /// the caller didn't supply a <see cref="HostedConversationClientOptions"/> instance, or a clone (via <see cref="HostedConversationClientOptions.Clone"/>) of the caller-supplied
    /// instance if one was supplied.
    /// </remarks>
    public ConfigureOptionsHostedConversationClient(IHostedConversationClient innerClient, Action<HostedConversationClientOptions> configure)
        : base(innerClient)
    {
        _configureOptions = Throw.IfNull(configure);
    }

    /// <inheritdoc/>
    public override async Task<HostedConversation> CreateAsync(
        HostedConversationClientOptions? options = null, CancellationToken cancellationToken = default)
    {
        return await base.CreateAsync(Configure(options), cancellationToken);
    }

    /// <inheritdoc/>
    public override async Task<HostedConversation> GetAsync(
        string conversationId, HostedConversationClientOptions? options = null, CancellationToken cancellationToken = default)
    {
        return await base.GetAsync(conversationId, Configure(options), cancellationToken);
    }

    /// <inheritdoc/>
    public override async Task DeleteAsync(
        string conversationId, HostedConversationClientOptions? options = null, CancellationToken cancellationToken = default)
    {
        await base.DeleteAsync(conversationId, Configure(options), cancellationToken);
    }

    /// <inheritdoc/>
    public override async Task AddMessagesAsync(
        string conversationId, IEnumerable<ChatMessage> messages, HostedConversationClientOptions? options = null, CancellationToken cancellationToken = default)
    {
        await base.AddMessagesAsync(conversationId, messages, Configure(options), cancellationToken);
    }

    /// <inheritdoc/>
    public override IAsyncEnumerable<ChatMessage> GetMessagesAsync(
        string conversationId, HostedConversationClientOptions? options = null, CancellationToken cancellationToken = default)
    {
        return base.GetMessagesAsync(conversationId, Configure(options), cancellationToken);
    }

    /// <summary>Creates and configures the <see cref="HostedConversationClientOptions"/> to pass along to the inner client.</summary>
    private HostedConversationClientOptions Configure(HostedConversationClientOptions? options)
    {
        options = options?.Clone() ?? new();

        _configureOptions(options);

        return options;
    }
}
