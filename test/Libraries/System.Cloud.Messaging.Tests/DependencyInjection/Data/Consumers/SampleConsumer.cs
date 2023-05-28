// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Shared.Diagnostics;

namespace System.Cloud.Messaging.DependencyInjection.Tests.Data.Consumers;

internal class SampleConsumer : MessageConsumer
{
    private bool _stopConsumer;

    public SampleConsumer(IMessageSource messageSource, IReadOnlyList<IMessageMiddleware> messageMiddlewares, MessageDelegate terminalDelegate, ILogger logger)
        : base(messageSource, messageMiddlewares, terminalDelegate, logger)
    {
        _stopConsumer = false;
    }

    /// <inheritdoc/>
    public override async ValueTask ExecuteAsync(CancellationToken cancellationToken)
    {
        while (!_stopConsumer)
        {
            await ProcessingStepAsync(CancellationToken.None).ConfigureAwait(false);
        }
    }

    protected override ValueTask HandleMessageProcessingFailureAsync(MessageContext context, Exception exception)
    {
        Throw.InvalidOperationException("Failure during processing.", exception);
        return default;
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Handled by HandleMessageProcessingFailureAsync.")]
    protected override async ValueTask ProcessingStepAsync(CancellationToken cancellationToken)
    {
        MessageContext messageContext = await FetchMessageAsync(CancellationToken.None).ConfigureAwait(false);

        try
        {
            if (ShouldStopConsumer(messageContext))
            {
                _stopConsumer = true;
                return;
            }

            await ProcessMessageAsync(messageContext).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            await HandleMessageProcessingFailureAsync(messageContext, exception).ConfigureAwait(false);
        }
    }

    protected override bool ShouldStopConsumer(MessageContext context)
    {
        var payload = context.GetUTF8SourcePayloadAsString();
        return string.IsNullOrEmpty(payload);
    }
}
