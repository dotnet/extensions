// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace System.Cloud.Messaging.Internal;

/// <summary>
/// Implements <see cref="IMessagePayloadFeature"/>.
/// </summary>
internal sealed class MessagePayloadFeature : IMessagePayloadFeature
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MessagePayloadFeature"/> class.
    /// </summary>
    /// <param name="payload"><see cref="byte"/> array of payload.</param>
    public MessagePayloadFeature(byte[] payload)
    {
        Payload = payload;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MessagePayloadFeature"/> class.
    /// </summary>
    /// <param name="payload">Payload.</param>
    public MessagePayloadFeature(ReadOnlyMemory<byte> payload)
    {
        Payload = payload;
    }

    /// <inheritdoc/>
    public ReadOnlyMemory<byte> Payload { get; }
}
