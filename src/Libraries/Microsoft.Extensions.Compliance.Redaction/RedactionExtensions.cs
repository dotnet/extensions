// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Compliance.Redaction;

/// <summary>
/// Add redaction to the application.
/// </summary>
public static partial class RedactionExtensions
{
    /// <summary>
    /// Registers redaction in the application.
    /// </summary>
    /// <param name="builder"><see cref="IHostBuilder"/> instance.</param>
    /// <returns>The value of <paramref name="builder"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <see langword="null"/>.</exception>
    public static IHostBuilder ConfigureRedaction(this IHostBuilder builder)
    {
        _ = Throw.IfNull(builder);

        return builder.ConfigureServices((_, services) => services.AddRedaction());
    }

    /// <summary>
    /// Registers redaction in the application.
    /// </summary>
    /// <param name="builder"><see cref="IHostBuilder"/> instance.</param>
    /// <param name="configure">Configuration for <see cref="IRedactionBuilder"/>.</param>
    /// <returns>The value of <paramref name="builder"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="configure"/> is <see langword="null"/>.</exception>
    public static IHostBuilder ConfigureRedaction(
        this IHostBuilder builder,
        Action<HostBuilderContext, IRedactionBuilder> configure)
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNull(configure);

        return builder.ConfigureServices((context, services) => services.AddRedaction(builder => configure(context, builder)));
    }

    /// <summary>
    /// Registers redaction in the application.
    /// </summary>
    /// <param name="builder"><see cref="IHostBuilder"/> instance.</param>
    /// <param name="configure">Configuration for <see cref="IRedactionBuilder"/>.</param>
    /// <returns>The value of <paramref name="builder"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="configure"/> is <see langword="null"/>.</exception>
    public static IHostBuilder ConfigureRedaction(this IHostBuilder builder, Action<IRedactionBuilder> configure)
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNull(configure);

        return builder.ConfigureServices((_, services) => services.AddRedaction(builder => configure(builder)));
    }

    /// <summary>
    /// Registers an implementation of <see cref="IRedactorProvider"/> in the <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">Instance of <see cref="IServiceCollection"/> used to configure redaction.</param>
    /// <returns>The value of <paramref name="services"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> is <see langword="null"/>.</exception>
    public static IServiceCollection AddRedaction(this IServiceCollection services)
    {
        _ = Throw.IfNull(services);

        return services.AddRedaction(_ => { });
    }

    /// <summary>
    /// Registers an implementation of <see cref="IRedactorProvider"/> in the <see cref="IServiceCollection"/> and configures available redactors.
    /// </summary>
    /// <param name="services">Instance of <see cref="IServiceCollection"/> used to configure redaction.</param>
    /// <param name="configure">Configuration function for <see cref="IRedactionBuilder"/>.</param>
    /// <returns>The value of <paramref name="services"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="configure"/> is <see langword="null"/>.</exception>
    public static IServiceCollection AddRedaction(this IServiceCollection services, Action<IRedactionBuilder> configure)
    {
        _ = Throw.IfNull(services);
        _ = Throw.IfNull(configure);

        services
            .AddOptions<RedactorProviderOptions>()
            .Services
            .TryAddSingleton<IRedactorProvider, RedactorProvider>();

        configure(new RedactionBuilder(services));

        return services;
    }
}
