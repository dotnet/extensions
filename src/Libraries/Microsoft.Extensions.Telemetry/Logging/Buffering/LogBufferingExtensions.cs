// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Diagnostics.Logging.Buffering;

/// <summary>
/// Lets you register log buffers in a dependency injection container.
/// </summary>
public static class LogBufferingExtensions
{
    /// <summary>
    /// Add log buffering.
    /// </summary>
    /// <param name="builder">An instance of <see cref="ILoggingBuilder"/> to enable buffering in.</param>
    /// <param name="configure">A delegate to fine-tune the buffering.</param>
    /// <returns>The value of <paramref name="builder"/>.</returns>
    public static ILoggingBuilder EnableBuffering(this ILoggingBuilder builder, Action<LogBufferingOptions> configure)
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNull(configure);

        builder.Services
            .Configure(configure)
            .TryAddActivatedSingleton<ILogBuffer, LogBuffer>();

        return builder;
    }

    /// <summary>
    /// Add log buffering.
    /// </summary>
    /// <param name="builder">An instance of <see cref="ILoggingBuilder"/> to enable buffering in.</param>
    /// <param name="section">Configuration section that contains <see cref="LogBufferingOptions"/>.</param>
    /// <returns>The value of <paramref name="builder"/>.</returns>
    public static ILoggingBuilder EnableBuffering(this ILoggingBuilder builder, IConfigurationSection section)
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNull(section);

        _ = builder.Services.AddOptions<LogBufferingOptions>().Bind(section);
        builder.Services.TryAddActivatedSingleton<ILogBuffer, LogBuffer>();

        return builder;
    }
}
