// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Cloud.Messaging.Internal;

/// <summary>
/// Implements <see cref="IMessagePayloadFeature"/>.
/// </summary>
internal sealed class SerializedMessagePayloadFeature<T> : ISerializedMessagePayloadFeature<T>
    where T : notnull
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SerializedMessagePayloadFeature{T}"/> class.
    /// </summary>
    /// <param name="payload"><typeparamref name="T"/> of payload.</param>
    public SerializedMessagePayloadFeature(T payload)
    {
        Payload = payload;
    }

    /// <inheritdoc/>
    public T Payload { get; }
}
