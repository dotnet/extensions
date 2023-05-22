// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Telemetry.Latency;
using Microsoft.Shared.Diagnostics;

namespace System.Cloud.Messaging;

/// <summary>
/// Extension methods for <see cref="MessageContext"/> for setting/retrieving <see cref="ILatencyContext"/>.
/// </summary>
public static class MessageLatencyContextFeatureExtensions
{
    /// <summary>
    /// Sets the <paramref name="latencyContext"/> in <paramref name="context"/>.
    /// </summary>
    /// <param name="context"><see cref="MessageContext"/>.</param>
    /// <param name="latencyContext"><see cref="ILatencyContext"/>.</param>
    /// <exception cref="ArgumentNullException">If any of the parameters is null.</exception>
    public static void SetLatencyContext(this MessageContext context, ILatencyContext latencyContext)
    {
        _ = Throw.IfNull(context);
        _ = Throw.IfNull(context.Features);
        _ = Throw.IfNull(latencyContext);

        context.Features.Set(latencyContext);
    }

    /// <summary>
    /// Try get the <paramref name="latencyContext"/> from the <paramref name="context"/>.
    /// </summary>
    /// <remarks>
    /// Application should set the features via the <see cref="SetLatencyContext(MessageContext, ILatencyContext)"/>.
    /// </remarks>
    /// <param name="context"><see cref="MessageContext"/>.</param>
    /// <param name="latencyContext"><see cref="ILatencyContext"/>.</param>
    /// <returns><see cref="bool"/> value indicating whether the <see cref="ILatencyContext"/> was obtained from the <paramref name="context"/>.</returns>
    /// <exception cref="ArgumentNullException">If any of the parameters is null.</exception>
    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Handled by TryGet pattern.")]
    public static bool TryGetLatencyContext(this MessageContext context, out ILatencyContext? latencyContext)
    {
        _ = Throw.IfNull(context);
        _ = Throw.IfNull(context.Features);

        try
        {
            latencyContext = context.Features.Get<ILatencyContext>();
            return latencyContext != null;
        }
        catch (Exception)
        {
            latencyContext = null;
            return false;
        }
    }
}
