// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Shared.Diagnostics;

namespace System.Cloud.Messaging;

/// <summary>
/// Extension methods for <see cref="MessageContext"/> for <see cref="IMessageCompleteActionFeature"/>.
/// </summary>
public static class MessageCompleteActionFeatureExtensions
{
    /// <summary>
    /// Marks the message processing as complete.
    /// </summary>
    /// <remarks>
    /// Implementation libraries should ensure to set the <see cref="IMessageCompleteActionFeature"/> via <see cref="SetMessageCompleteActionFeature(MessageContext, IMessageCompleteActionFeature)"/>
    /// typically in their <see cref="IMessageSource"/> implementations.
    /// </remarks>
    /// <param name="context"><see cref="MessageContext"/>.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>.</param>
    /// <returns><see cref="ValueTask"/>.</returns>
    /// <exception cref="ArgumentNullException">If any of the parameters is null.</exception>
    public static ValueTask MarkCompleteAsync(this MessageContext context, CancellationToken cancellationToken)
    {
        _ = Throw.IfNull(context);
        _ = Throw.IfNull(context.Features);
        _ = Throw.IfNull(cancellationToken);

        IMessageCompleteActionFeature? feature = context.Features.Get<IMessageCompleteActionFeature>();
        _ = Throw.IfNull(feature);

        return feature.MarkCompleteAsync(cancellationToken);
    }

    /// <summary>
    /// Sets the <see cref="IMessageCompleteActionFeature"/> to the <see cref="MessageContext"/>.
    /// </summary>
    /// <param name="context"><see cref="MessageContext"/>.</param>
    /// <param name="messageCompleteActionFeature"><see cref="IMessageCompleteActionFeature"/>.</param>
    /// <exception cref="ArgumentNullException">If any of the parameters is null.</exception>
    public static void SetMessageCompleteActionFeature(this MessageContext context, IMessageCompleteActionFeature messageCompleteActionFeature)
    {
        _ = Throw.IfNull(context);
        _ = Throw.IfNull(context.Features);
        _ = Throw.IfNull(messageCompleteActionFeature);

        context.Features.Set(messageCompleteActionFeature);
    }
}
