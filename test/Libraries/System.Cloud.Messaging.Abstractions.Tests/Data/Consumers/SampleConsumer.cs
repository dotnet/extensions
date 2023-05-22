// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace System.Cloud.Messaging.Tests.Data.Consumers;

internal class SampleConsumer : IMessageConsumer
{
    private readonly IMessageSource _messageSource;
    private readonly IMessageDelegate _messageDelegate;

    public SampleConsumer(IMessageSource messageSource, IMessageDelegate messageDelegate)
    {
        _messageSource = messageSource;
        _messageDelegate = messageDelegate;
    }

    /// <inheritdoc/>
    public async ValueTask ExecuteAsync(CancellationToken cancellationToken)
    {
        while (true)
        {
            try
            {
                MessageContext messageContext = await _messageSource.ReadAsync(CancellationToken.None);

                _ = messageContext.TryGetSourcePayloadAsUTF8String(out string message);
                if (string.IsNullOrEmpty(message))
                {
                    return;
                }

                await _messageDelegate.InvokeAsync(messageContext);
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException("Failure during processing.", exception);
            }
        }
    }
}
