// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Microsoft.Shared.Diagnostics;

namespace System.Cloud.Messaging.DependencyInjection.Tests.Data.Middlewares;

internal class SampleMiddleware : IMessageMiddleware
{
    public MessageDelegate MessageDelegate { get; }

    public SampleMiddleware(MessageDelegate messageDelegate)
    {
        MessageDelegate = Throw.IfNull(messageDelegate);
    }

    /// <inheritdoc/>
    public async ValueTask InvokeAsync(MessageContext context, MessageDelegate nextHandler)
    {
        await MessageDelegate.Invoke(context).ConfigureAwait(false);
        await nextHandler.Invoke(context).ConfigureAwait(false);
    }
}
