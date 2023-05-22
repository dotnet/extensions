// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace System.Cloud.Messaging.Tests.Data.Consumers;

internal class DerivedConsumer : BaseMessageConsumer
{
    public DerivedConsumer(IMessageSource source, IMessageDelegate messageDelegate, ILogger logger)
        : base(source, messageDelegate, logger)
    {
    }

    /// <inheritdoc/>
    protected override ValueTask OnMessageProcessingFailureAsync(MessageContext context, Exception exception) => default;
}
