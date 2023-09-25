// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Compliance.Redaction;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extensions to register redaction functionality.
/// </summary>
public static class RedactionServiceCollectionExtensions
{
    /// <summary>
    /// Registers an implementation of <see cref="IRedactorProvider"/> in the <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">Instance of <see cref="IServiceCollection"/> used to configure redaction.</param>
    /// <returns>The value of <paramref name="services"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> is <see langword="null"/>.</exception>
    public static IServiceCollection AddRedaction(this IServiceCollection services)
        => Throw.IfNull(services).AddRedaction(_ => { });

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
