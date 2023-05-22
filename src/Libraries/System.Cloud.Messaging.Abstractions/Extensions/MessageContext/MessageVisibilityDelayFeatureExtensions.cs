// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Cloud.Messaging.Internal;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Shared.Diagnostics;

namespace System.Cloud.Messaging;

/// <summary>
/// Extension methods for <see cref="MessageContext"/> for setting and retrieving visibility delay using <see cref="IMessageVisibilityDelayFeature"/>.
/// </summary>
public static class MessageVisibilityDelayFeatureExtensions
{
    /// <summary>
    /// Sets <see cref="IMessageVisibilityDelayFeature"/> with the provided <paramref name="visibilityDelay"/> in the <see cref="MessageContext"/>.
    /// </summary>
    /// <param name="context"><see cref="MessageContext"/>.</param>
    /// <param name="visibilityDelay"><see cref="TimeSpan"/>.</param>
    /// <exception cref="ArgumentNullException">If any of the arguments is null.</exception>
    public static void SetVisibilityDelay(this MessageContext context, TimeSpan visibilityDelay)
    {
        _ = context.TryGetMessageSourceFeatures(out IFeatureCollection? sourceFeatures);
        sourceFeatures ??= new FeatureCollection();

        sourceFeatures.Set<IMessageVisibilityDelayFeature>(new MessageVisibilityDelayFeature(visibilityDelay));
        context.SetMessageSourceFeatures(sourceFeatures);
    }

    /// <summary>
    /// Try to get <see cref="IMessageVisibilityDelayFeature"/> in the provided <paramref name="visibilityDelay"/> from the <see cref="MessageContext"/>.
    /// </summary>
    /// <param name="context"><see cref="MessageContext"/>.</param>
    /// <param name="visibilityDelay"><see cref="TimeSpan"/>.</param>
    /// <returns><see cref="bool"/> and if <see langword="true"/>, a corresponding <see cref="IMessageVisibilityDelayFeature"/>.</returns>
    /// <exception cref="ArgumentNullException">If any of the arguments is null.</exception>
    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Handled by TryGet pattern.")]
    public static bool TryGetVisibilityDelay(this MessageContext context, out IMessageVisibilityDelayFeature? visibilityDelay)
    {
        _ = context.TryGetMessageSourceFeatures(out IFeatureCollection? sourceFeatures);
        _ = Throw.IfNull(sourceFeatures);

        try
        {
            visibilityDelay = sourceFeatures.Get<IMessageVisibilityDelayFeature>();
            return visibilityDelay != null;
        }
        catch (Exception)
        {
            visibilityDelay = null;
            return false;
        }
    }
}
