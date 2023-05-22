// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;

namespace System.Cloud.Messaging.Tests.Data.Middlewares;

internal class SampleMiddleware : IMessageMiddleware
{
    public IMessageDelegate MessageDelegate { get; }

    public SampleMiddleware(IMessageDelegate messageDelegate)
    {
        MessageDelegate = messageDelegate;
    }

    /// <inheritdoc/>
    public async ValueTask InvokeAsync(MessageContext context, IMessageDelegate nextHandler)
    {
        await MessageDelegate.InvokeAsync(context).ConfigureAwait(false);
        await nextHandler.InvokeAsync(context).ConfigureAwait(false);
    }
}
