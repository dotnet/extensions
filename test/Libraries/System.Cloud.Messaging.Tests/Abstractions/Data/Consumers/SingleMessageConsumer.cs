// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Shared.Diagnostics;

namespace System.Cloud.Messaging.Tests.Data.Consumers;

internal class SingleMessageConsumer : MessageConsumer
{
    private readonly bool _shouldThrowExceptionDuringHandlingCompletion;
    private readonly bool _shouldThrowExceptionDuringHandlingFailure;

    public SingleMessageConsumer(IMessageSource source,
                                 IReadOnlyList<IMessageMiddleware> middlewares,
                                 MessageDelegate terminalDelegate,
                                 ILogger logger,
                                 bool shouldThrowExceptionDuringHandlingCompletion = false,
                                 bool shouldThrowExceptionDuringHandlingFailure = false)
    : base(source, middlewares, terminalDelegate, logger)
    {
        _shouldThrowExceptionDuringHandlingCompletion = shouldThrowExceptionDuringHandlingCompletion;
        _shouldThrowExceptionDuringHandlingFailure = shouldThrowExceptionDuringHandlingFailure;
    }

    public override ValueTask ExecuteAsync(CancellationToken cancellationToken) => ProcessingStepAsync(CancellationToken.None);

    /// <inheritdoc/>
    protected override ValueTask HandleMessageProcessingCompletionAsync(MessageContext context)
    {
        if (_shouldThrowExceptionDuringHandlingCompletion)
        {
            Throw.InvalidOperationException("Exception thrown during handling completion.");
        }

        return default;
    }

    /// <inheritdoc/>
    protected override ValueTask HandleMessageProcessingFailureAsync(MessageContext context, Exception exception)
    {
        if (_shouldThrowExceptionDuringHandlingFailure)
        {
            Throw.InvalidOperationException("Exception thrown during handling failure.");
        }

        return default;
    }

    protected override ValueTask ProcessingStepAsync(CancellationToken cancellationToken) => FetchAndProcessMessageAsync(cancellationToken);
}
