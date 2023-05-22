// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http.Features;

namespace System.Cloud.Messaging.Internal;

/// <summary>
/// Implements <see cref="IMessageSourceFeatures"/>.
/// </summary>
internal sealed class MessageSourceFeatures : IMessageSourceFeatures
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MessageSourceFeatures"/> class.
    /// </summary>
    /// <param name="features"><see cref="IFeatureCollection"/>.</param>
    public MessageSourceFeatures(IFeatureCollection features)
    {
        Features = features;
    }

    /// <inheritdoc/>
    public IFeatureCollection Features { get; }
}
