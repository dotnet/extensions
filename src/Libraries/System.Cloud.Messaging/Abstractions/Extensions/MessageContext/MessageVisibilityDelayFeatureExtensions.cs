// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Cloud.Messaging.Internal;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Shared.Diagnostics;

namespace System.Cloud.Messaging;

/// <summary>
/// Provides extension methods for <see cref="MessageContext"/> to add support for <see cref="IMessageVisibilityDelayFeature"/>.
/// </summary>
public static class MessageVisibilityDelayFeatureExtensions
{
    /// <summary>
    /// Sets <see cref="IMessageVisibilityDelayFeature"/> with the provided <paramref name="visibilityDelay"/> in the <see cref="MessageContext"/>.
    /// </summary>
    /// <param name="context">The message context.</param>
    /// <param name="visibilityDelay">The time span representing when the message should be next visible for processing via a different <see cref="MessageConsumer"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="context"/> is <see langword="null"/>.</exception>
    public static void SetVisibilityDelay(this MessageContext context, TimeSpan visibilityDelay)
    {
        _ = Throw.IfNull(context);
        context.AddSourceFeature<IMessageVisibilityDelayFeature>(new MessageVisibilityDelayFeature(visibilityDelay));
    }

    /// <summary>
    /// Tries to get <see cref="IMessageVisibilityDelayFeature"/> in the provided <paramref name="visibilityDelay"/> from the <see cref="MessageContext"/>.
    /// </summary>
    /// <param name="context">The message context.</param>
    /// <param name="visibilityDelay">The optional feature to delay the message visibility.</param>
    /// <returns><see cref="bool"/> and if <see langword="true"/>, a corresponding <see cref="IMessageVisibilityDelayFeature"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="context"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">No source <see cref="IFeatureCollection"/> is added to <paramref name="context"/>.</exception>
    public static bool TryGetVisibilityDelay(this MessageContext context, [NotNullWhen(true)] out IMessageVisibilityDelayFeature? visibilityDelay)
    {
        _ = Throw.IfNull(context);

        visibilityDelay = context.SourceFeatures?.Get<IMessageVisibilityDelayFeature>();
        return visibilityDelay != null;
    }
}
