// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.DiagnosticIds;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>
/// A delegating chat client that makes an <see cref="IHostedConversationClient"/> discoverable
/// via <see cref="IChatClient.GetService(Type, object?)"/>.
/// </summary>
/// <remarks>
/// This middleware passes through all chat operations unchanged. Its sole purpose is to hold a reference
/// to an <see cref="IHostedConversationClient"/> and return it when requested through the service discovery mechanism.
/// </remarks>
[Experimental(DiagnosticIds.Experiments.AIHostedConversation, UrlFormat = DiagnosticIds.UrlFormat)]
public sealed class HostedConversationChatClient : DelegatingChatClient
{
#pragma warning disable CA2213 // Disposable fields should be disposed - not owned by this instance
    private readonly IHostedConversationClient _hostedConversationClient;
#pragma warning restore CA2213

    /// <summary>Initializes a new instance of the <see cref="HostedConversationChatClient"/> class.</summary>
    /// <param name="innerClient">The inner <see cref="IChatClient"/>.</param>
    /// <param name="hostedConversationClient">The <see cref="IHostedConversationClient"/> to make discoverable.</param>
    public HostedConversationChatClient(IChatClient innerClient, IHostedConversationClient hostedConversationClient)
        : base(innerClient)
    {
        _hostedConversationClient = Throw.IfNull(hostedConversationClient);
    }

    /// <inheritdoc/>
    public override object? GetService(Type serviceType, object? serviceKey = null)
    {
        _ = Throw.IfNull(serviceType);

        if (serviceKey is null && serviceType.IsInstanceOfType(_hostedConversationClient))
        {
            return _hostedConversationClient;
        }

        return base.GetService(serviceType, serviceKey);
    }
}
