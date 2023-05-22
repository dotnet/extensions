// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Cloud.Messaging.Internal;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Shared.Diagnostics;

namespace System.Cloud.Messaging;

/// <summary>
/// Extension methods for <see cref="MessageContext"/> to be used for <see cref="IMessageDestination"/>.
/// </summary>
public static class MessageDestinationFeatureExtensions
{
    /// <summary>
    /// Sets the <paramref name="destinationFeatures"/> in <paramref name="context"/>.
    /// </summary>
    /// <remarks>
    /// Implementation libraries should set the features to <paramref name="context"/> in their <see cref="IMessageDestination"/> implementations.
    /// </remarks>
    /// <param name="context"><see cref="MessageContext"/>.</param>
    /// <param name="destinationFeatures"><see cref="IFeatureCollection"/>.</param>
    /// <exception cref="ArgumentNullException">If any of the parameters is null.</exception>
    public static void SetMessageDestinationFeatures(this MessageContext context, IFeatureCollection destinationFeatures)
    {
        _ = Throw.IfNull(context);
        _ = Throw.IfNull(context.Features);
        _ = Throw.IfNull(destinationFeatures);

        context.Features.Set<IMessageDestinationFeatures>(new MessageDestinationFeatures(destinationFeatures));
    }

    /// <summary>
    /// Try get the <paramref name="destinationFeatures"/> from the <paramref name="context"/>.
    /// </summary>
    /// <remarks>
    /// Implementation libraries should set the features via the <see cref="SetMessageDestinationFeatures(MessageContext, IFeatureCollection)"/>.
    /// </remarks>
    /// <param name="context"><see cref="MessageContext"/>.</param>
    /// <param name="destinationFeatures"><see cref="IFeatureCollection"/>.</param>
    /// <returns><see cref="bool"/> value indicating whether the source features was obtained from the <paramref name="context"/>.</returns>
    /// <exception cref="ArgumentNullException">If any of the parameters is null.</exception>
    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Handled by TryGet pattern.")]
    public static bool TryGetMessageDestinationFeatures(this MessageContext context, out IFeatureCollection? destinationFeatures)
    {
        _ = Throw.IfNull(context);
        _ = Throw.IfNull(context.Features);

        try
        {
            destinationFeatures = context.Features.Get<IMessageDestinationFeatures>()?.Features;
            return destinationFeatures != null;
        }
        catch (Exception)
        {
            destinationFeatures = null;
            return false;
        }
    }
}
