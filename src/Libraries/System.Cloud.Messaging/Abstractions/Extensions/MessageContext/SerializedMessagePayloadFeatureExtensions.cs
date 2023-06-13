// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Cloud.Messaging.Internal;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.Diagnostics;

namespace System.Cloud.Messaging;

/// <summary>
/// Provides extension methods for <see cref="MessageContext"/> class to add support for <see cref="ISerializedMessagePayloadFeature{T}"/>.
/// </summary>
public static class SerializedMessagePayloadFeatureExtensions
{
    /// <summary>
    /// Gets the message payload as a serialized <typeparamref name="T"/> type.
    /// </summary>
    /// <remarks>
    /// Ensure the serialized message payload is set in the pipeline via <see cref="SetSerializedPayload{T}(MessageContext, T)"/> before calling this method.
    /// </remarks>
    /// <typeparam name="T">Type of the serialized message payload.</typeparam>
    /// <param name="context">The message context.</param>
    /// <returns>The serialized message payload.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="context"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">No <see cref="ISerializedMessagePayloadFeature{T}"/> is set in the <paramref name="context"/>.</exception>
    public static T GetSerializedPayload<T>(this MessageContext context)
        where T : notnull
    {
        _ = Throw.IfNullOrMemberNull(context, context?.Features);

        ISerializedMessagePayloadFeature<T>? feature = context.Features.Get<ISerializedMessagePayloadFeature<T>>();
        if (feature == null)
        {
            Throw.InvalidOperationException(ExceptionMessages.NoSerializedMessagePayloadFeatureOnMessageContext);
        }

        return feature!.Payload;
    }

    /// <summary>
    /// Sets the message payload in the <see cref="MessageContext"/> as a serialized <typeparamref name="T"/> type.
    /// </summary>
    /// <typeparam name="T">Type of the serialized message payload.</typeparam>
    /// <param name="context">The message context.</param>
    /// <param name="payload">The serialized message payload.</param>
    /// <exception cref="ArgumentNullException">Any of the parameters are <see langword="null"/>.</exception>
    public static void SetSerializedPayload<T>(this MessageContext context, T payload)
        where T : notnull
    {
        _ = Throw.IfNullOrMemberNull(context, context?.Features);
        context.AddFeature<ISerializedMessagePayloadFeature<T>>(new SerializedMessagePayloadFeature<T>(payload));
    }

    /// <summary>
    /// Try to get the serialized message payload of <typeparamref name="T"/> type from the <see cref="MessageContext"/>.
    /// </summary>
    /// <typeparam name="T">Type of the serialized message.</typeparam>
    /// <param name="context">The message context.</param>
    /// <param name="payload">The optional serialized message payload.</param>
    /// <returns><see cref="bool"/> and if <see langword="true"/>, a corresponding <typeparamref name="T"/> representing the serialized message payload.</returns>
    /// <exception cref="ArgumentNullException">Any of the parameters are <see langword="null"/>.</exception>
    public static bool TryGetSerializedPayload<T>(this MessageContext context, [NotNullWhen(true)] out T? payload)
        where T : notnull
    {
        _ = Throw.IfNullOrMemberNull(context, context?.Features);

        ISerializedMessagePayloadFeature<T>? feature = context.Features.Get<ISerializedMessagePayloadFeature<T>>();
        if (feature == null)
        {
            payload = default;
            return false;
        }
        else
        {
            payload = feature!.Payload;
            return true;
        }
    }
}
