// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace System.Cloud.Messaging.Middlewares.Tests.Data.Consumers;

internal class SingleMessageConsumer : MessageConsumer
{
    public SingleMessageConsumer(IMessageSource source,
                                 IReadOnlyList<IMessageMiddleware> messageMiddlewares,
                                 MessageDelegate terminalDelegate,
                                 ILogger logger)
    : base(source, messageMiddlewares, terminalDelegate, logger)
    {
    }

    public override ValueTask ExecuteAsync(CancellationToken cancellationToken) => ProcessingStepAsync(CancellationToken.None);

    /// <inheritdoc/>
    protected override ValueTask HandleMessageProcessingFailureAsync(MessageContext context, Exception exception) => default;

    protected override ValueTask ProcessingStepAsync(CancellationToken cancellationToken) => FetchAndProcessMessageAsync(cancellationToken);
}
