// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using Microsoft.Extensions.Compliance.Classification;
using Microsoft.Extensions.Compliance.Redaction;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience.Internal;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Http.Resilience;

/// <summary>
/// Extensions for <see cref="IHttpStandardResilienceStrategyBuilder"/>.
/// </summary>
public static class HttpStandardResilienceBuilderBuilderExtensions
{
    /// <summary>
    /// Configures the <see cref="HttpStandardResilienceOptions"/> for the standard resilience strategy.
    /// </summary>
    /// <param name="builder">The strategy builder.</param>
    /// <param name="section">The section that the options will bind against.</param>
    /// <returns>The same builder instance.</returns>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(HttpStandardResilienceOptions))]
    public static IHttpStandardResilienceStrategyBuilder Configure(this IHttpStandardResilienceStrategyBuilder builder, IConfigurationSection section)
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNull(section);
        var options = Throw.IfNull(section.Get<HttpStandardResilienceOptions>());

        _ = builder.Services.Configure<HttpStandardResilienceOptions>(
            builder.StrategyName,
            section,
            o => o.ErrorOnUnknownConfiguration = true);

        return builder;
    }

    /// <summary>
    /// Configures the <see cref="HttpStandardResilienceOptions"/> for the standard resilience strategy.
    /// </summary>
    /// <param name="builder">The strategy builder.</param>
    /// <param name="configure">The configure method.</param>
    /// <returns>The same builder instance.</returns>
#pragma warning disable S3872 // Parameter names should not duplicate the names of their methods
    public static IHttpStandardResilienceStrategyBuilder Configure(this IHttpStandardResilienceStrategyBuilder builder, Action<HttpStandardResilienceOptions> configure)
#pragma warning restore S3872 // Parameter names should not duplicate the names of their methods
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNull(configure);

        return builder.Configure((options, _) => configure(options));
    }

    /// <summary>
    /// Configures the <see cref="HttpStandardResilienceOptions"/> for the standard resilience strategy.
    /// </summary>
    /// <param name="builder">The strategy builder.</param>
    /// <param name="configure">The configure method.</param>
    /// <returns>The same builder instance.</returns>
#pragma warning disable S3872 // Parameter names should not duplicate the names of their methods
    [Experimental(diagnosticId: "TBD", UrlFormat = "TBD")]
    public static IHttpStandardResilienceStrategyBuilder Configure(this IHttpStandardResilienceStrategyBuilder builder, Action<HttpStandardResilienceOptions, IServiceProvider> configure)
#pragma warning restore S3872 // Parameter names should not duplicate the names of their methods
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNull(configure);

        _ = builder.Services.AddOptions<HttpStandardResilienceOptions>(builder.StrategyName).Configure(configure);

        return builder;
    }

    /// <summary>
    /// Instructs the underlying builder to select the strategy instance by redacted authority (scheme + host + port).
    /// </summary>
    /// <param name="builder">The strategy builder.</param>
    /// <param name="classification">The data class associated with the authority.</param>
    /// <returns>The same builder instance.</returns>
    /// <remarks>The authority is redacted using <see cref="Redactor"/> retrieved for <paramref name="classification"/>.</remarks>
    public static IHttpStandardResilienceStrategyBuilder SelectStrategyByAuthority(this IHttpStandardResilienceStrategyBuilder builder, DataClassification classification)
    {
        _ = Throw.IfNull(builder);

        StrategyKeyProviderHelper.SelectStrategyByAuthority(builder.Services, builder.StrategyName, classification);

        return builder;
    }

    /// <summary>
    /// Instructs the underlying builder to select the strategy instance by custom selector.
    /// </summary>
    /// <param name="builder">The strategy builder.</param>
    /// <param name="selectorFactory">The factory that returns a key selector.</param>
    /// <returns>The same builder instance.</returns>
    /// <remarks>The pipeline key is used in metrics and logs, do not return any sensitive value.</remarks>
    public static IHttpStandardResilienceStrategyBuilder SelectStrategyBy(this IHttpStandardResilienceStrategyBuilder builder, Func<IServiceProvider, Func<HttpRequestMessage, string>> selectorFactory)
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNull(selectorFactory);

        StrategyKeyProviderHelper.SelectStrategyBy(builder.Services, builder.StrategyName, selectorFactory);

        return builder;
    }
}
