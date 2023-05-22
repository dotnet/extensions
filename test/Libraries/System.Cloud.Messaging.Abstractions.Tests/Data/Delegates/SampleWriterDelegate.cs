// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;

namespace System.Cloud.Messaging.Tests.Data.Delegates;

internal class SampleWriterDelegate : IMessageDelegate
{
    public IMessageDestination MessageDestination { get; }

    public SampleWriterDelegate(IMessageDestination messageWriter)
    {
        MessageDestination = messageWriter;
    }

    /// <inheritdoc/>
    public async ValueTask InvokeAsync(MessageContext context)
    {
        await MessageDestination.WriteAsync(context);
    }
}
