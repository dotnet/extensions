// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Compliance.Redaction;
using Microsoft.Extensions.Compliance.Testing;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extensions that allow registering a fake redactor in the application.
/// </summary>
public static class FakeRedactionServiceCollectionExtensions
{
    /// <summary>
    /// Registers the fake redactor provider that always returns fake redactor instances.
    /// </summary>
    /// <param name="services">Container used to register fake redaction classes.</param>
    /// <returns>The value of <paramref name="services" />.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> is <see langword="null"/>.</exception>
    public static IServiceCollection AddFakeRedaction(this IServiceCollection services)
    {
        _ = Throw.IfNull(services);

        services.TryAddSingleton<FakeRedactionCollector>();
        services.TryAddSingleton<IRedactorProvider>(serviceProvider =>
        {
            var collector = serviceProvider.GetRequiredService<FakeRedactionCollector>();
            var options = serviceProvider.GetRequiredService<IOptions<FakeRedactorOptions>>().Value;
            return new FakeRedactorProvider(options, collector);
        });

        return services
            .AddOptionsWithValidateOnStart<FakeRedactorOptions, FakeRedactorOptionsAutoValidator>()
            .Services.AddOptionsWithValidateOnStart<FakeRedactorOptions, FakeRedactorOptionsCustomValidator>()
            .Services;
    }

    /// <summary>
    /// Registers the fake redactor provider that always returns fake redactor instances.
    /// </summary>
    /// <param name="services">Container used to register fake redaction classes.</param>
    /// <param name="configure">Configures fake redactor.</param>
    /// <returns>The value of <paramref name="services" />.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> or <paramref name="configure"/>> are <see langword="null"/>.</exception>
    public static IServiceCollection AddFakeRedaction(this IServiceCollection services, Action<FakeRedactorOptions> configure)
    {
        _ = Throw.IfNull(services);
        _ = Throw.IfNull(configure);

        services.TryAddSingleton<FakeRedactionCollector>();
        services.TryAddSingleton<IRedactorProvider>(serviceProvider =>
        {
            var collector = serviceProvider.GetRequiredService<FakeRedactionCollector>();
            var options = serviceProvider.GetRequiredService<IOptions<FakeRedactorOptions>>().Value;

            return new FakeRedactorProvider(options, collector);
        });

        return services
            .AddOptionsWithValidateOnStart<FakeRedactorOptions, FakeRedactorOptionsAutoValidator>()
            .Services.AddOptionsWithValidateOnStart<FakeRedactorOptions, FakeRedactorOptionsCustomValidator>()
            .Configure(configure)
            .Services;
    }
}
