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

#pragma warning disable S3872 // Parameter names should not duplicate the names of their methods

/// <summary>
/// Extensions for <see cref="IStandardHedgingHandlerBuilder"/>.
/// </summary>
public static class StandardHedgingHandlerBuilderExtensions
{
    /// <summary>
    /// Configures the <see cref="HttpStandardHedgingResilienceOptions"/> for the standard hedging strategy.
    /// </summary>
    /// <param name="builder">The strategy builder.</param>
    /// <param name="section">The section that the options will bind against.</param>
    /// <returns>The same builder instance.</returns>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(HttpStandardHedgingResilienceOptions))]
    public static IStandardHedgingHandlerBuilder Configure(this IStandardHedgingHandlerBuilder builder, IConfigurationSection section)
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNull(section);
        var options = Throw.IfNull(section.Get<HttpStandardHedgingResilienceOptions>());

        _ = builder.Services.Configure<HttpStandardHedgingResilienceOptions>(
            builder.Name,
            section,
            o => o.ErrorOnUnknownConfiguration = true);

        return builder;
    }

    /// <summary>
    /// Configures the <see cref="HttpStandardResilienceOptions"/> for the standard hedging strategy.
    /// </summary>
    /// <param name="builder">The strategy builder.</param>
    /// <param name="configure">The configure method.</param>
    /// <returns>The same builder instance.</returns>
    public static IStandardHedgingHandlerBuilder Configure(this IStandardHedgingHandlerBuilder builder, Action<HttpStandardHedgingResilienceOptions> configure)
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNull(configure);

        return builder.Configure((options, _) => configure(options));
    }

    /// <summary>
    /// Configures the <see cref="HttpStandardResilienceOptions"/> for the standard hedging strategy.
    /// </summary>
    /// <param name="builder">The strategy builder.</param>
    /// <param name="configure">The configure method.</param>
    /// <returns>The same builder instance.</returns>
    [Experimental(diagnosticId: "TBD", UrlFormat = WarningDefinitions.SharedUrlFormat)]
    public static IStandardHedgingHandlerBuilder Configure(this IStandardHedgingHandlerBuilder builder, Action<HttpStandardHedgingResilienceOptions, IServiceProvider> configure)
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNull(configure);

        _ = builder.Services.AddOptions<HttpStandardHedgingResilienceOptions>(builder.Name).Configure(configure);

        return builder;
    }

    /// <summary>
    /// Instructs the underlying strategy builder to select the strategy instance by redacted authority (scheme + host + port).
    /// </summary>
    /// <param name="builder">The builder instance.</param>
    /// <param name="classification">The data class associated with the authority.</param>
    /// <returns>The same builder instance.</returns>
    /// <remarks>The authority is redacted using <see cref="Redactor"/> retrieved for <paramref name="classification"/>.</remarks>
    public static IStandardHedgingHandlerBuilder SelectStrategyByAuthority(this IStandardHedgingHandlerBuilder builder, DataClassification classification)
    {
        _ = Throw.IfNull(builder);

        var strategyName = StrategyNameHelper.GetName(builder.Name, HttpClientBuilderExtensions.StandardInnerHandlerPostfix);

        StrategyKeyProviderHelper.SelectStrategyByAuthority(builder.Services, strategyName, classification);

        return builder;
    }

    /// <summary>
    /// Instructs the underlying strategy builder to select the strategy instance by custom selector.
    /// </summary>
    /// <param name="builder">The builder instance.</param>
    /// <param name="selectorFactory">The factory that returns key selector.</param>
    /// <returns>The same builder instance.</returns>
    /// <remarks>The strategy key is used in metrics and logs, do not return any sensitive value.</remarks>
    public static IStandardHedgingHandlerBuilder SelectStrategyBy(this IStandardHedgingHandlerBuilder builder, Func<IServiceProvider, Func<HttpRequestMessage, string>> selectorFactory)
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNull(selectorFactory);

        var strategyName = StrategyNameHelper.GetName(builder.Name, HttpClientBuilderExtensions.StandardInnerHandlerPostfix);

        StrategyKeyProviderHelper.SelectStrategyBy(builder.Services, strategyName, selectorFactory);

        return builder;
    }
}
