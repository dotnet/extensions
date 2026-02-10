// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Provides extension methods for attaching a <see cref="FunctionInvokingRealtimeSession"/> to a realtime session pipeline.
/// </summary>
[Experimental("MEAI001")]
public static class FunctionInvokingRealtimeSessionBuilderExtensions
{
    /// <summary>
    /// Enables automatic function call invocation on the realtime session pipeline.
    /// </summary>
    /// <remarks>This works by adding an instance of <see cref="FunctionInvokingRealtimeSession"/> with default options.</remarks>
    /// <param name="builder">The <see cref="RealtimeSessionBuilder"/> being used to build the realtime session pipeline.</param>
    /// <param name="loggerFactory">An optional <see cref="ILoggerFactory"/> to use to create a logger for logging function invocations.</param>
    /// <param name="configure">An optional callback that can be used to configure the <see cref="FunctionInvokingRealtimeSession"/> instance.</param>
    /// <returns>The supplied <paramref name="builder"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <see langword="null"/>.</exception>
    public static RealtimeSessionBuilder UseFunctionInvocation(
        this RealtimeSessionBuilder builder,
        ILoggerFactory? loggerFactory = null,
        Action<FunctionInvokingRealtimeSession>? configure = null)
    {
        _ = Throw.IfNull(builder);

        return builder.Use((innerSession, services) =>
        {
            loggerFactory ??= services.GetService<ILoggerFactory>();

            var realtimeSession = new FunctionInvokingRealtimeSession(innerSession, loggerFactory, services);
            configure?.Invoke(realtimeSession);
            return realtimeSession;
        });
    }
}
