// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Compliance.Classification;
using Microsoft.Extensions.Compliance.Redaction;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Options.Validation;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Compliance.Testing;

/// <summary>
/// Extensions that allow registering a fake redactor in the application.
/// </summary>
public static class FakeRedactionExtensions
{
    /// <summary>
    /// Sets the fake redactor to use for a set of data classes.
    /// </summary>
    /// <param name="builder">The builder to attach the redactorr to.</param>
    /// <param name="classifications">The data classes for which the redactor type should be used.</param>
    /// <returns>The value of <paramref name="builder" />.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <see langword="null"/>.</exception>
    public static IRedactionBuilder SetFakeRedactor(this IRedactionBuilder builder, params DataClassification[] classifications)
    {
        _ = Throw.IfNull(builder);

        builder.Services.TryAddSingleton<FakeRedactionCollector>();

        return builder.SetRedactor<FakeRedactor>(classifications);
    }

    /// <summary>
    /// Sets the fake redactor to use for a set of data classes.
    /// </summary>
    /// <param name="builder">The builder to attach the redactorr to.</param>
    /// <param name="configure">Configuration function.</param>
    /// <param name="classifications">The data classes for which the redactor type should be used.</param>
    /// <returns>The value of <paramref name="builder" />.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> or <paramref name="configure"/> are <see langword="null"/>.</exception>
    public static IRedactionBuilder SetFakeRedactor(this IRedactionBuilder builder, Action<FakeRedactorOptions> configure, params DataClassification[] classifications)
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNull(configure);

        builder
            .Services.AddValidatedOptions<FakeRedactorOptions, FakeRedactorOptionsAutoValidator>()
            .Services.AddValidatedOptions<FakeRedactorOptions, FakeRedactorOptionsCustomValidator>()
            .Configure(configure)
            .Services.TryAddSingleton<FakeRedactionCollector>();

        return builder.SetRedactor<FakeRedactor>(classifications);
    }

    /// <summary>
    /// Sets the fake redactor to use for a set of data classes.
    /// </summary>
    /// <param name="builder">The builder to attach the redactorr to.</param>
    /// <param name="section">Configuration section.</param>
    /// <param name="classifications">The data classes for which the redactor type should be used.</param>
    /// <returns>The value of <paramref name="builder" />.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> or <paramref name="section"/> are <see langword="null"/>.</exception>
    [UnconditionalSuppressMessage("Trimming",
        "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code",
        Justification = "The type is FakeRedactorOptions and we know it.")]
    public static IRedactionBuilder SetFakeRedactor(this IRedactionBuilder builder, IConfigurationSection section, params DataClassification[] classifications)
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNull(section);

        builder
            .Services.AddValidatedOptions<FakeRedactorOptions, FakeRedactorOptionsAutoValidator>()
            .Services.AddValidatedOptions<FakeRedactorOptions, FakeRedactorOptionsCustomValidator>()
            .Services.Configure<FakeRedactorOptions>(section)
            .TryAddSingleton<FakeRedactionCollector>();

        return builder.SetRedactor<FakeRedactor>(classifications);
    }

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
            .AddValidatedOptions<FakeRedactorOptions, FakeRedactorOptionsAutoValidator>()
            .Services.AddValidatedOptions<FakeRedactorOptions, FakeRedactorOptionsCustomValidator>()
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
            .AddValidatedOptions<FakeRedactorOptions, FakeRedactorOptionsAutoValidator>()
            .Services.AddValidatedOptions<FakeRedactorOptions, FakeRedactorOptionsCustomValidator>()
            .Configure(configure)
            .Services;
    }

    /// <summary>
    /// Gets the fake redactor collector instance from the dependency injection container.
    /// </summary>
    /// <param name="serviceProvider">The container used to obtain the collector instance.</param>
    /// <returns>The obtained collector.</returns>
    /// <exception cref="InvalidOperationException">The collector is not in the container.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="serviceProvider"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// <see cref="FakeRedactionCollector"/> should be registered and used only with fake redaction implementation.
    /// </remarks>
    public static FakeRedactionCollector GetFakeRedactionCollector(this IServiceProvider serviceProvider)
    {
        _ = Throw.IfNull(serviceProvider);
        return serviceProvider.GetRequiredService<FakeRedactionCollector>();
    }
}
