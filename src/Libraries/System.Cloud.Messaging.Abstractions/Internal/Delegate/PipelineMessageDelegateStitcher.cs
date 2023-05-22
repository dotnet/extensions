// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading.Tasks;
using Microsoft.Shared.Diagnostics;

namespace System.Cloud.Messaging.Internal;

/// <summary>
/// Utility class to combine the <see cref="IMessageMiddleware"/> and the next <see cref="IMessageDelegate"/> to create an equivalent <see cref="IMessageDelegate"/>.
/// </summary>
internal sealed class PipelineMessageDelegateStitcher : IMessageDelegate
{
    private readonly IMessageMiddleware _middleware;
    private readonly IMessageDelegate _nextHandler;

    /// <summary>
    /// Initializes a new instance of the <see cref="PipelineMessageDelegateStitcher"/> class.
    /// </summary>
    /// <param name="middleware"><see cref="IMessageMiddleware"/>.</param>
    /// <param name="nextHandler"><see cref="IMessageDelegate"/>.</param>
    /// <exception cref="ArgumentNullException">If any of the parameters is null.</exception>
    public PipelineMessageDelegateStitcher(IMessageMiddleware middleware, IMessageDelegate nextHandler)
    {
        _middleware = Throw.IfNull(middleware);
        _nextHandler = Throw.IfNull(nextHandler);
    }

    /// <inheritdoc/>
    public ValueTask InvokeAsync(MessageContext context)
    {
        return _middleware.InvokeAsync(context, _nextHandler);
    }
}
