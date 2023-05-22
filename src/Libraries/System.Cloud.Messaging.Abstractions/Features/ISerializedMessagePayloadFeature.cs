// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Cloud.Messaging;

/// <summary>
/// Feature interface for setting/retrieving the serialized message payload.
/// </summary>
/// <typeparam name="T">Type of the message payload.</typeparam>
public interface ISerializedMessagePayloadFeature<out T>
    where T : notnull
{
    /// <summary>
    /// Gets the message payload.
    /// </summary>
    T Payload { get; }
}
