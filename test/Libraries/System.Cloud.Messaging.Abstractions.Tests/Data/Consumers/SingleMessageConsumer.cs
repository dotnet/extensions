// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace System.Cloud.Messaging.Tests.Data.Consumers;

internal class SingleMessageConsumer : BaseMessageConsumer
{
    private readonly bool _shouldThrowExceptionDuringHandlingCompletion;
    private readonly bool _shouldThrowExceptionDuringHandlingFailure;

    public SingleMessageConsumer(IMessageSource source,
                                 IMessageDelegate messageDelegate,
                                 ILogger logger,
                                 bool shouldThrowExceptionDuringHandlingCompletion = false,
                                 bool shouldThrowExceptionDuringHandlingFailure = false)
    : base(source, messageDelegate, logger)
    {
        _shouldThrowExceptionDuringHandlingCompletion = shouldThrowExceptionDuringHandlingCompletion;
        _shouldThrowExceptionDuringHandlingFailure = shouldThrowExceptionDuringHandlingFailure;
    }

    public override async ValueTask ExecuteAsync(CancellationToken cancellationToken)
    {
        await FetchAndProcessMessageAsync(cancellationToken);
    }

    /// <inheritdoc/>
    protected override ValueTask OnMessageProcessingCompletionAsync(MessageContext context)
    {
        if (_shouldThrowExceptionDuringHandlingCompletion)
        {
            throw new InvalidOperationException("Exception thrown during handling completion.");
        }

        return default;
    }

    /// <inheritdoc/>
    protected override ValueTask OnMessageProcessingFailureAsync(MessageContext context, Exception exception)
    {
        if (_shouldThrowExceptionDuringHandlingFailure)
        {
            throw new InvalidOperationException("Exception thrown during handling failure.");
        }

        return default;
    }
}
