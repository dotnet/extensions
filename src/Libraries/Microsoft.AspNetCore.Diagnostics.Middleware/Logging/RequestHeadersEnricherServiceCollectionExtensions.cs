// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Diagnostics.Logging;
using Microsoft.Extensions.Compliance.Classification;
using Microsoft.Extensions.Configuration;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for setting up Request Headers Log Enricher in an <see cref="IServiceCollection" />.
/// </summary>
public static class RequestHeadersEnricherServiceCollectionExtensions
{
    /// <summary>
    /// Adds an instance of Request Headers Log Enricher to the <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the Request Headers Log Enricher to.</param>
    /// <returns>The value of <paramref name="services"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> is <see langword="null" />.</exception>
    public static IServiceCollection AddRequestHeadersLogEnricher(this IServiceCollection services)
        => Throw.IfNull(services)
        .AddLogEnricherOptions(_ => { })
        .RegisterRequestHeadersEnricher();

    /// <summary>
    /// Adds an instance of Request Headers Log Enricher to the <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the Request Headers Log Enricher to.</param>
    /// <param name="configure">The <see cref="RequestHeadersLogEnricherOptions"/> configuration delegate.</param>
    /// <returns>The value of <paramref name="services"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> is <see langword="null" />.</exception>
    public static IServiceCollection AddRequestHeadersLogEnricher(this IServiceCollection services, Action<RequestHeadersLogEnricherOptions> configure)
    {
        _ = Throw.IfNull(services);
        _ = Throw.IfNull(configure);

        return services
            .AddLogEnricherOptions(configure)
            .RegisterRequestHeadersEnricher();
    }

    /// <summary>
    /// Adds an instance of Request Headers Log Enricher to the <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the Request Headers Log Enricher to.</param>
    /// <param name="section">The <see cref="IConfigurationSection"/> to use for configuring <see cref="RequestHeadersLogEnricherOptions"/>
    /// in the Request Headers Log Enricher.</param>
    /// <returns>The value of <paramref name="services"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> is <see langword="null" />.</exception>
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicParameterlessConstructor, typeof(RequestHeadersLogEnricherOptions))]
    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code",
        Justification = "Addressed with [DynamicDependency]")]
    public static IServiceCollection AddRequestHeadersLogEnricher(this IServiceCollection services, IConfigurationSection section)
    {
        _ = Throw.IfNull(services);
        _ = Throw.IfNull(section);

        return services
            .AddLogEnricherOptions(o =>
            {
                var requestHeaders = section.GetSection(nameof(RequestHeadersLogEnricherOptions.HeadersDataClasses));
                foreach (var entry in requestHeaders.GetChildren())
                {
                    var taxonomy = entry.GetValue<string>(nameof(DataClassification.TaxonomyName));
                    var value = entry.GetValue<ulong>(nameof(DataClassification.Value));
                    if (taxonomy != null)
                    {
                        o.HeadersDataClasses.Add(entry.Key, new DataClassification(taxonomy, value));
                    }
                }
            })
            .RegisterRequestHeadersEnricher();
    }

    private static IServiceCollection RegisterRequestHeadersEnricher(this IServiceCollection services)
        => services
        .AddHttpContextAccessor()
        .AddLogEnricher<RequestHeadersLogEnricher>();

    private static IServiceCollection AddLogEnricherOptions(this IServiceCollection services, Action<RequestHeadersLogEnricherOptions> configure)
        => services
        .AddOptionsWithValidateOnStart<RequestHeadersLogEnricherOptions, RequestHeadersLogEnricherOptionsValidator>()
        .Configure(configure)
        .Services;
}
