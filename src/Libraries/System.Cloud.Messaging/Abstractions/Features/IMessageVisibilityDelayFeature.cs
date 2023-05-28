// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace System.Cloud.Messaging;

/// <summary>
/// Feature interface for setting/retrieving the visibility delay.
/// </summary>
public interface IMessageVisibilityDelayFeature
{
    /// <summary>
    /// Gets the visibility delay which represents the delay after which the message is available for other <see cref="MessageConsumer"/> to process.
    /// </summary>
    TimeSpan VisibilityDelay { get; }
}
