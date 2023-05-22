// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Cloud.Messaging.Internal;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Shared.Diagnostics;

namespace System.Cloud.Messaging;

/// <summary>
/// Extension methods for <see cref="MessageContext"/> used during <see cref="IMessageSource"/> for <see cref="IMessageSourceFeatures"/>.
/// </summary>
public static class MessageSourceFeatureExtensions
{
    /// <summary>
    /// Sets the <paramref name="sourceFeatures"/> in <paramref name="context"/>.
    /// </summary>
    /// <remarks>
    /// Implementation libraries should set the features to <paramref name="context"/> in their <see cref="IMessageSource"/> implementations.
    /// </remarks>
    /// <param name="context"><see cref="MessageContext"/>.</param>
    /// <param name="sourceFeatures"><see cref="IFeatureCollection"/>.</param>
    /// <exception cref="ArgumentNullException">If any of the parameters is null.</exception>
    public static void SetMessageSourceFeatures(this MessageContext context, IFeatureCollection sourceFeatures)
    {
        _ = Throw.IfNull(context);
        _ = Throw.IfNull(context.Features);
        _ = Throw.IfNull(sourceFeatures);

        context.Features.Set<IMessageSourceFeatures>(new MessageSourceFeatures(sourceFeatures));
    }

    /// <summary>
    /// Try get the <paramref name="sourceFeatures"/> from the <paramref name="context"/>.
    /// </summary>
    /// <remarks>
    /// Implementation libraries should set the features via the <see cref="SetMessageSourceFeatures(MessageContext, IFeatureCollection)"/>.
    /// </remarks>
    /// <param name="context"><see cref="MessageContext"/>.</param>
    /// <param name="sourceFeatures"><see cref="IFeatureCollection"/>.</param>
    /// <returns><see cref="bool"/> value indicating whether the source features was obtained from the <paramref name="context"/>.</returns>
    /// <exception cref="ArgumentNullException">If any of the parameters is null.</exception>
    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Handled by TryGet pattern.")]
    public static bool TryGetMessageSourceFeatures(this MessageContext context, out IFeatureCollection? sourceFeatures)
    {
        _ = Throw.IfNull(context);
        _ = Throw.IfNull(context.Features);

        try
        {
            sourceFeatures = context.Features.Get<IMessageSourceFeatures>()?.Features;
            return sourceFeatures != null;
        }
        catch (Exception)
        {
            sourceFeatures = null;
            return false;
        }
    }
}
