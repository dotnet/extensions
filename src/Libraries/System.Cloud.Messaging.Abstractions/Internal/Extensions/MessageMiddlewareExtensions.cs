// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Shared.Diagnostics;

namespace System.Cloud.Messaging.Internal;

/// <summary>
/// Extension methods for <see cref="IMessageMiddleware"/>.
/// </summary>
internal static class MessageMiddlewareExtensions
{
    /// <summary>
    /// Generate a composable <see cref="IMessageDelegate"/> from the <see cref="IMessageMiddleware"/> and <see cref="IMessageDelegate"/>.
    /// </summary>
    /// <param name="middleware"><see cref="IMessageMiddleware"/>.</param>
    /// <param name="nextHandler"><see cref="IMessageDelegate"/>.</param>
    /// <returns>Combined <see cref="IMessageDelegate"/>.</returns>
    /// <exception cref="ArgumentNullException">If any of the parameters is null.</exception>
    public static IMessageDelegate Stitch(this IMessageMiddleware middleware, IMessageDelegate nextHandler)
    {
        _ = Throw.IfNull(middleware);
        _ = Throw.IfNull(nextHandler);

        return new PipelineMessageDelegateStitcher(middleware, nextHandler);
    }
}
