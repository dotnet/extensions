// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Shared.Diagnostics;

namespace System.Cloud.Messaging.Internal;

/// <summary>
/// Provides extension methods for <see cref="IMessageMiddleware"/> interface to add support for constructing a composable <see cref="MessageDelegate"/>
/// from the provided <see cref="IMessageMiddleware"/> and <see cref="MessageDelegate"/>.
/// </summary>
internal static class MessageMiddlewareExtensions
{
    /// <summary>
    /// Generate a composable <see cref="MessageDelegate"/> from the provided <see cref="IMessageMiddleware"/> and <see cref="MessageDelegate"/>.
    /// </summary>
    /// <param name="middleware">The middleware.</param>
    /// <param name="nextHandler">The next delegate in the pipeline.</param>
    /// <returns>Composed message handler.</returns>
    /// <exception cref="ArgumentNullException">Any of the parameters are <see langword="null"/>.</exception>
    public static MessageDelegate Stitch(this IMessageMiddleware middleware, MessageDelegate nextHandler)
    {
        _ = Throw.IfNull(middleware);
        _ = Throw.IfNull(nextHandler);

        return new PipelineMessageDelegateStitcher(middleware, nextHandler).InvokeAsync;
    }
}
