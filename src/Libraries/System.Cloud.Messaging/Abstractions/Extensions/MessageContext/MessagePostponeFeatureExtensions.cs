// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Cloud.Messaging.Internal;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Shared.Diagnostics;

namespace System.Cloud.Messaging;

/// <summary>
/// Provides extension methods for <see cref="MessageContext"/> class to add support for <see cref="IMessagePostponeFeature"/>.
/// </summary>
public static class MessagePostponeFeatureExtensions
{
    /// <summary>
    /// Postpones the message processing asynchronously.
    /// </summary>
    /// <remarks>
    /// Implementation libraries should ensure to set the <see cref="IMessagePostponeFeature"/> via <see cref="SetMessagePostponeFeature(MessageContext, IMessagePostponeFeature)"/>
    /// typically in their <see cref="IMessageSource"/> implementations.
    /// </remarks>
    /// <param name="context">The message context.</param>
    /// <param name="delay">The time by which the message processing is to be postponed.</param>
    /// <param name="cancellationToken">The cancellation token for the postpone operation.</param>
    /// <returns>To be added.</returns>
    /// <exception cref="ArgumentNullException">Any of the parameters are <see langword="null" />.</exception>
    /// <exception cref="InvalidOperationException">There is no <see cref="IMessagePostponeFeature"/> added to <paramref name="context"/>.</exception>
    public static ValueTask PostponeAsync(this MessageContext context, TimeSpan delay, CancellationToken cancellationToken)
    {
        _ = Throw.IfNullOrMemberNull(context, context?.Features);

        IMessagePostponeFeature feature = context.Features.Get<IMessagePostponeFeature>()
            ?? throw new InvalidOperationException(ExceptionMessages.NoMessagePostponeFeatureOnMessageContext);

        return feature.PostponeAsync(delay, cancellationToken);
    }

    /// <summary>
    /// Sets the <see cref="IMessagePostponeFeature"/> in the <see cref="MessageContext"/>.
    /// </summary>
    /// <param name="context">The message context.</param>
    /// <param name="messagePostponeFeature">The feature to postpone message processing.</param>
    /// <exception cref="ArgumentNullException">Any of the parameters are <see langword="null"/>.</exception>
    public static void SetMessagePostponeFeature(this MessageContext context, IMessagePostponeFeature messagePostponeFeature)
    {
        _ = Throw.IfNull(context);
        context.AddSourceFeature(messagePostponeFeature);
    }
}
