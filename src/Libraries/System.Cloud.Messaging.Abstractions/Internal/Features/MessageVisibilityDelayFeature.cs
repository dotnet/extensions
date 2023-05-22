// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace System.Cloud.Messaging.Internal;

/// <summary>
/// Implements <see cref="IMessageVisibilityDelayFeature"/>.
/// </summary>
internal sealed class MessageVisibilityDelayFeature : IMessageVisibilityDelayFeature
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MessageVisibilityDelayFeature"/> class.
    /// </summary>
    /// <param name="visibilityDelay"><see cref="TimeSpan"/> representing visibility delay.</param>
    public MessageVisibilityDelayFeature(TimeSpan visibilityDelay)
    {
        VisibilityDelay = visibilityDelay;
    }

    /// <inheritdoc/>
    public TimeSpan VisibilityDelay { get; }
}
