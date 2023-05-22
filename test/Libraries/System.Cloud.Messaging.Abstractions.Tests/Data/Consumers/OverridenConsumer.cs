// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace System.Cloud.Messaging.Tests.Data.Consumers;

internal class OverridenConsumer : BaseMessageConsumer
{
    public OverridenConsumer(IMessageSource source, IMessageDelegate messageDelegate, ILogger logger)
        : base(source, messageDelegate, logger)
    {
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = $"Handled by {nameof(OnMessageProcessingFailureAsync)}")]
    public override async ValueTask ExecuteAsync(CancellationToken cancellationToken)
    {
        while (true)
        {
            try
            {
                MessageContext messageContext = await MessageSource.ReadAsync(CancellationToken.None);

                _ = messageContext.TryGetSourcePayloadAsUTF8String(out string message);
                if (string.IsNullOrEmpty(message))
                {
                    return;
                }

                try
                {
                    await MessageDelegate.InvokeAsync(messageContext);
                    await OnMessageProcessingCompletionAsync(messageContext);
                }
                catch (Exception exception)
                {
                    await OnMessageProcessingFailureAsync(messageContext, exception);
                }
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException("Failure during processing.", exception);
            }
        }
    }

    /// <inheritdoc/>
    protected override ValueTask OnMessageProcessingFailureAsync(MessageContext context, Exception exception) => default;
}
