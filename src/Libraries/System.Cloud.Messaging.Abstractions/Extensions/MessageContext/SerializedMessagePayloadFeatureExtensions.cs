// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Cloud.Messaging.Internal;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Shared.Diagnostics;

namespace System.Cloud.Messaging;

/// <summary>
/// Extension methods for <see cref="MessageContext"/> obtained from <see cref="IMessageSource"/> for <see cref="ISerializedMessagePayloadFeature{T}"/>.
/// </summary>
public static class SerializedMessagePayloadFeatureExtensions
{
    /// <summary>
    /// Gets the message payload obtained from <see cref="IMessageSource"/>.
    /// </summary>
    /// <remarks>
    /// Ensure the serialized message payload is set in the pipeline via <see cref="SetSerializedPayload{T}(MessageContext, T)"/> before calling this method.
    /// </remarks>
    /// <typeparam name="T">Type of the serialized message payload.</typeparam>
    /// <param name="context"><see cref="MessageContext"/>.</param>
    /// <returns>Message payload as <see cref="ReadOnlyMemory{T}"/>.</returns>
    public static T GetSerializedPayload<T>(this MessageContext context)
        where T : notnull
    {
        _ = context.TryGetMessageSourceFeatures(out IFeatureCollection? sourceFeatures);
        _ = Throw.IfNull(sourceFeatures);

        var feature = sourceFeatures.Get<ISerializedMessagePayloadFeature<T>>();
        _ = Throw.IfNull(feature);

        return feature!.Payload;
    }

    /// <summary>
    /// Sets the message payload in the <see cref="MessageContext"/> obtained from <see cref="IMessageSource"/>.
    /// </summary>
    /// <typeparam name="T">Type of the serialized message payload.</typeparam>
    /// <param name="context"><see cref="MessageContext"/>.</param>
    /// <param name="payload">Message payload in <see cref="ReadOnlyMemory{T}"/> of <see cref="byte"/>.</param>
    public static void SetSerializedPayload<T>(this MessageContext context, T payload)
        where T : notnull
    {
        _ = Throw.IfNull(payload);

        _ = context.TryGetMessageSourceFeatures(out IFeatureCollection? sourceFeatures);
        sourceFeatures ??= new FeatureCollection();

        sourceFeatures.Set<ISerializedMessagePayloadFeature<T>>(new SerializedMessagePayloadFeature<T>(payload));
        context.SetMessageSourceFeatures(sourceFeatures);
    }

    /// <summary>
    /// Try to get the message payload obtained from <see cref="IMessageSource"/>.
    /// </summary>
    /// <typeparam name="T">Type of the serialized message.</typeparam>
    /// <param name="context"><see cref="MessageContext"/>.</param>
    /// <param name="payload">The <see langword="out"/> to store the <see cref="ReadOnlyMemory{T}"/> representing the message payload.</param>
    /// <returns><see cref="bool"/> and if <see langword="true"/>, a corresponding <see cref="ReadOnlyMemory{T}"/>.</returns>
    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Handled by TryGet pattern.")]
    public static bool TryGetSerializedPayload<T>(this MessageContext context, out T? payload)
        where T : notnull
    {
        try
        {
            payload = context.GetSerializedPayload<T>();
            return true;
        }
        catch (Exception)
        {
            payload = default;
            return false;
        }
    }
}
