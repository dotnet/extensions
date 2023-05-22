// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Shared.Diagnostics;

namespace System.Cloud.Messaging;

/// <summary>
/// Extension methods for <see cref="MessageContext"/> for <see cref="IMessagePostponeActionFeature"/>.
/// </summary>
public static class MessagePostponeActionFeatureExtensions
{
    /// <summary>
    /// Postpones the message processing asynchronously.
    /// </summary>
    /// <remarks>
    /// Implementation libraries should ensure to set the <see cref="IMessagePostponeActionFeature"/> via <see cref="SetMessagePostponeActionFeature(MessageContext, IMessagePostponeActionFeature)"/>
    /// typically in their <see cref="IMessageSource"/> implementations.
    /// </remarks>
    /// <param name="context"><see cref="MessageContext"/>.</param>
    /// <param name="delay"><see cref="TimeSpan"/> by which message processing is to be delayed.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>.</param>
    /// <returns><see cref="ValueTask"/>.</returns>
    /// <exception cref="ArgumentNullException">If any of the parameters is null.</exception>
    public static ValueTask PostponeAsync(this MessageContext context, TimeSpan delay, CancellationToken cancellationToken)
    {
        _ = Throw.IfNull(context);
        _ = Throw.IfNull(context.Features);
        _ = Throw.IfNull(cancellationToken);

        IMessagePostponeActionFeature? feature = context.Features.Get<IMessagePostponeActionFeature>();
        _ = Throw.IfNull(feature);

        return feature.PostponeAsync(delay, cancellationToken);
    }

    /// <summary>
    /// Sets the <see cref="IMessagePostponeActionFeature"/> to the <see cref="MessageContext"/>.
    /// </summary>
    /// <param name="context"><see cref="MessageContext"/>.</param>
    /// <param name="messagePostponeActionFeature"><see cref="IMessagePostponeActionFeature"/>.</param>
    /// <exception cref="ArgumentNullException">If any of the parameters is null.</exception>
    public static void SetMessagePostponeActionFeature(this MessageContext context, IMessagePostponeActionFeature messagePostponeActionFeature)
    {
        _ = Throw.IfNull(context);
        _ = Throw.IfNull(context.Features);
        _ = Throw.IfNull(messagePostponeActionFeature);

        context.Features.Set(messagePostponeActionFeature);
    }
}
