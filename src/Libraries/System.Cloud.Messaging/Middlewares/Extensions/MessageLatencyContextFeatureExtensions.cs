// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Telemetry.Latency;
using Microsoft.Shared.Diagnostics;

namespace System.Cloud.Messaging;

/// <summary>
/// Provides extension methods for <see cref="MessageContext"/> class to add support for setting/retrieving <see cref="ILatencyContext"/>.
/// </summary>
public static class MessageLatencyContextFeatureExtensions
{
    /// <summary>
    /// Sets the <see cref="ILatencyContext"/> in <see cref="MessageContext"/>.
    /// </summary>
    /// <remarks>
    /// The <see cref="ILatencyContext"/> allows user to set fine-grained latency and associated properties for different processing steps.
    /// </remarks>
    /// <param name="context">The message context.</param>
    /// <param name="latencyContext">The latency context to store fine-grained latency for different processing steps.</param>
    /// <exception cref="ArgumentNullException">Any of the parameters are <see langword="null"/>.</exception>
    public static void SetLatencyContext(this MessageContext context, ILatencyContext latencyContext)
    {
        _ = Throw.IfNullOrMemberNull(context, context?.Features);
        _ = Throw.IfNull(latencyContext);

        context.Features.Set(latencyContext);
    }

    /// <summary>
    /// Try to get the <see cref="ILatencyContext"/> from the <see cref="MessageContext"/>.
    /// </summary>
    /// <remarks>
    /// Application should set the <see cref="ILatencyContext"/> in the <see cref="MessageContext"/> via the <see cref="SetLatencyContext(MessageContext, ILatencyContext)"/>.
    /// </remarks>
    /// <param name="context">The message context.</param>
    /// <param name="latencyContext">The optional latency context registered with the <paramref name="context"/>.</param>
    /// <returns><see cref="bool"/> and if <see langword="true"/>, a corresponding <see cref="ILatencyContext"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="context"/> is <see langword="null"/>.</exception>
    public static bool TryGetLatencyContext(this MessageContext context, [NotNullWhen(true)] out ILatencyContext? latencyContext)
    {
        _ = Throw.IfNullOrMemberNull(context, context?.Features);

        latencyContext = context.Features.Get<ILatencyContext>();
        return latencyContext != null;
    }
}
