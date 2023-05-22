// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace System.Cloud.Messaging;

/// <summary>
/// Feature interface for setting/retrieving the message payload.
/// </summary>
public interface IMessagePayloadFeature
{
    /// <summary>
    /// Gets the message payload.
    /// </summary>
    ReadOnlyMemory<byte> Payload { get; }
}
