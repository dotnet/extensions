// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Features;

namespace System.Cloud.Messaging.Tests.Data.Sources;

internal class AnotherSource : IMessageSource
{
    private static MessageContext CreateContext(IFeatureCollection sourceFeatures)
    {
        var context = new MessageContext(new FeatureCollection());
        context.SetMessageSourceFeatures(sourceFeatures);
        return context;
    }

    private int _count;

    public string[] Messages { get; }

    public AnotherSource(string[] messages)
    {
        Messages = messages;
        _count = 0;
    }

    /// <inheritdoc/>
    public ValueTask<MessageContext> ReadAsync(CancellationToken cancellationToken)
    {
        if (_count < Messages.Length)
        {
            var message = Messages[_count];
            Interlocked.Increment(ref _count);

            MessageContext messageContext = CreateContext(new FeatureCollection());
            messageContext.SetSourcePayload(Encoding.UTF8.GetBytes(message));
            return new(messageContext);
        }

        var emptyContext = CreateContext(new FeatureCollection());
        emptyContext.SetSourcePayload(Encoding.UTF8.GetBytes(string.Empty));
        return new(emptyContext);
    }

    /// <inheritdoc/>
    public void Release(MessageContext context)
    {
        // No-op: Intentionally left empty.
    }
}
