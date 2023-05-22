// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Microsoft.Shared.Diagnostics;

namespace System.Cloud.Messaging;

/// <summary>
/// Extension methods for <see cref="MessageContext"/> for setting/retrieving <see cref="CancellationTokenSource"/>.
/// </summary>
public static class MessageCancelledTokenFeatureExtensions
{
    /// <summary>
    /// Sets the <see cref="CancellationTokenSource"/> and the corresponding <see cref="CancellationToken"/> in the <see cref="MessageContext.MessageCancelledToken"/>.
    /// </summary>
    /// <param name="context"><see cref="MessageContext"/>.</param>
    /// <param name="cancellationTokenSource"><see cref="CancellationTokenSource"/>.</param>
    /// <exception cref="ArgumentNullException">If any of the parameters is null.</exception>
    public static void SetMessageCancelledTokenSource(this MessageContext context, CancellationTokenSource cancellationTokenSource)
    {
        _ = Throw.IfNull(context);
        _ = Throw.IfNull(context.Features);
        _ = Throw.IfNull(cancellationTokenSource);

        context.Features.Set(cancellationTokenSource);
        context.MessageCancelledToken = cancellationTokenSource.Token;
    }

    /// <summary>
    /// Try to get the <see cref="CancellationTokenSource"/> for the <see cref="MessageContext.MessageCancelledToken"/>.
    /// </summary>
    /// <param name="context"><see cref="MessageContext"/>.</param>
    /// <param name="cancellationTokenSource">The <see langword="out"/> to store the <see cref="CancellationTokenSource"/> representing the message payload.</param>
    /// <returns><see cref="bool"/> and if <see langword="true"/>, a corresponding <see cref="CancellationTokenSource"/>.</returns>
    /// <exception cref="ArgumentNullException">If any of the parameters is null.</exception>
    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Handled by TryGet pattern.")]
    public static bool TryGetMessageCancelledTokenSource(this MessageContext context, out CancellationTokenSource? cancellationTokenSource)
    {
        _ = Throw.IfNull(context);
        _ = Throw.IfNull(context.Features);

        try
        {
            cancellationTokenSource = context.Features.Get<CancellationTokenSource>();
            return cancellationTokenSource != null;
        }
        catch (Exception)
        {
            cancellationTokenSource = null;
            return false;
        }
    }
}
