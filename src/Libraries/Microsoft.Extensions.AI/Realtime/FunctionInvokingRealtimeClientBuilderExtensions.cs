// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Shared.DiagnosticIds;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Provides extension methods for attaching function invocation middleware to a realtime client pipeline.
/// </summary>
[Experimental(DiagnosticIds.Experiments.AIRealTime, UrlFormat = DiagnosticIds.UrlFormat)]
public static class FunctionInvokingRealtimeClientBuilderExtensions
{
    /// <summary>
    /// Enables automatic function call invocation on the realtime client pipeline.
    /// </summary>
    /// <param name="builder">The <see cref="RealtimeClientBuilder"/> being used to build the realtime client pipeline.</param>
    /// <param name="loggerFactory">An optional <see cref="ILoggerFactory"/> to use to create a logger for logging function invocations.</param>
    /// <param name="configure">An optional callback that can be used to configure the <see cref="FunctionInvokingRealtimeClient"/> instance.</param>
    /// <returns>The supplied <paramref name="builder"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <see langword="null"/>.</exception>
    public static RealtimeClientBuilder UseFunctionInvocation(
        this RealtimeClientBuilder builder,
        ILoggerFactory? loggerFactory = null,
        Action<FunctionInvokingRealtimeClient>? configure = null)
    {
        _ = Throw.IfNull(builder);

        return builder.Use((innerClient, services) =>
        {
            loggerFactory ??= services.GetService<ILoggerFactory>();

            var client = new FunctionInvokingRealtimeClient(innerClient, loggerFactory, services);
            configure?.Invoke(client);
            return client;
        });
    }
}
