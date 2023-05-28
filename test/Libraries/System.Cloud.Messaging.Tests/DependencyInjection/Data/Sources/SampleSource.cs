// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Shared.Diagnostics;

namespace System.Cloud.Messaging.DependencyInjection.Tests.Data.Sources;

internal class SampleSource : IMessageSource
{
    private static MessageContext CreateContext(IFeatureCollection features, ReadOnlyMemory<byte> sourcePayload) => new TestMessageContext(features, sourcePayload);

    private readonly object _messageLock = new();
    private int _count;

    public string[] Messages { get; }

    public SampleSource(string[] messages)
    {
        Messages = Throw.IfNull(messages);
        _count = messages.Length;
    }

    /// <inheritdoc/>
    public ValueTask<MessageContext> ReadAsync(CancellationToken cancellationToken)
    {
        lock (_messageLock)
        {
            MessageContext messageContext;
            if (_count > 0)
            {
                var message = Messages[Interlocked.Decrement(ref _count)];
                messageContext = CreateContext(new FeatureCollection(), Encoding.UTF8.GetBytes(message));
            }
            else
            {
                messageContext = CreateContext(new FeatureCollection(), Encoding.UTF8.GetBytes(string.Empty));
            }

            return new ValueTask<MessageContext>(messageContext);
        }
    }

    /// <inheritdoc/>
    public void Release(MessageContext context)
    {
        // No-op: Intentionally left empty.
    }
}
