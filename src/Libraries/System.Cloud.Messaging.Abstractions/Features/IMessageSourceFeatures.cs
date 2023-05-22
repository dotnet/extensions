// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http.Features;

namespace System.Cloud.Messaging;

/// <summary>
/// Interface for <see cref="MessageContext"/> features read from <see cref="IMessageSource"/>.
/// </summary>
public interface IMessageSourceFeatures
{
    /// <summary>
    /// Gets the associated <see cref="IFeatureCollection"/>.
    /// </summary>
    public IFeatureCollection Features { get; }
}
