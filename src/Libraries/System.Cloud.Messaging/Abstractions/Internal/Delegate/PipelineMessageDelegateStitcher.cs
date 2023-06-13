// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Shared.Diagnostics;

namespace System.Cloud.Messaging.Internal;

/// <summary>
/// Utility class to combine the <see cref="IMessageMiddleware"/> and the next <see cref="MessageDelegate"/> to create an equivalent <see cref="MessageDelegate"/>.
/// </summary>
internal sealed class PipelineMessageDelegateStitcher
{
    private readonly IMessageMiddleware _middleware;
    private readonly MessageDelegate _nextHandler;

    /// <summary>
    /// Initializes a new instance of the <see cref="PipelineMessageDelegateStitcher"/> class.
    /// </summary>
    /// <param name="middleware"><see cref="IMessageMiddleware"/>.</param>
    /// <param name="nextHandler"><see cref="MessageDelegate"/>.</param>
    /// <exception cref="ArgumentNullException">Any of the parameters are <see langword="null"/>.</exception>
    public PipelineMessageDelegateStitcher(IMessageMiddleware middleware, MessageDelegate nextHandler)
    {
        _middleware = Throw.IfNull(middleware);
        _nextHandler = Throw.IfNull(nextHandler);
    }

    /// <summary>
    /// The <see cref="MessageDelegate"/> function.
    /// </summary>
    /// <param name="context">The message context.</param>
    /// <returns><see cref="ValueTask"/>.</returns>
    [SuppressMessage("Resilience", "R9A061:The async method doesn't support cancellation", Justification = $"{nameof(MessageContext)} has {nameof(CancellationToken)}")]
    public ValueTask InvokeAsync(MessageContext context)
    {
        return _middleware.InvokeAsync(context, _nextHandler);
    }
}
