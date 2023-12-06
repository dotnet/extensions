// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Logging;

/// <summary>
/// Extensions for configuring fake logging, used in unit tests.
/// </summary>
public static class FakeLoggerBuilderExtensions
{
    /// <summary>
    /// Configures fake logging.
    /// </summary>
    /// <param name="builder">Logging builder.</param>
    /// <param name="section">Configuration section that contains <see cref="FakeLogCollectorOptions"/>.</param>
    /// <returns>Logging <paramref name="builder"/>.</returns>
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicParameterlessConstructor, typeof(FakeLogCollectorOptions))]
    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code",
        Justification = "Addressed by [DynamicDependency]")]
    public static ILoggingBuilder AddFakeLogging(this ILoggingBuilder builder, IConfigurationSection section)
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNull(section);

        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, FakeLoggerProvider>());
        _ = builder.Services.Configure<FakeLogCollectorOptions>(section);
        _ = builder.Services.AddSingleton<FakeLogCollector>();

        return builder;
    }

    /// <summary>
    /// Configures fake logging.
    /// </summary>
    /// <param name="builder">Logging builder.</param>
    /// <param name="configure">Logging configuration options.</param>
    /// <returns>Logging <paramref name="builder"/>.</returns>
    public static ILoggingBuilder AddFakeLogging(this ILoggingBuilder builder, Action<FakeLogCollectorOptions> configure)
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNull(configure);

        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, FakeLoggerProvider>());
        _ = builder.Services.Configure(configure);
        _ = builder.Services.AddSingleton<FakeLogCollector>();

        return builder;
    }

    /// <summary>
    /// Configures fake logging with default options.
    /// </summary>
    /// <param name="builder">Logging builder.</param>
    /// <returns>Logging <paramref name="builder"/>.</returns>
    public static ILoggingBuilder AddFakeLogging(this ILoggingBuilder builder)
        => builder.AddFakeLogging(_ => { });
}
