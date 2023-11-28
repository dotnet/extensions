// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Net.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience.Hedging.Internals;
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
    /// Configures the <see cref="HttpStandardHedgingResilienceOptions"/> for the standard hedging pipeline.
    /// </summary>
    /// <param name="builder">The pipeline builder.</param>
    /// <param name="section">The section that the options will bind against.</param>
    /// <returns>The value of <paramref name="builder"/>.</returns>
    public static IStandardHedgingHandlerBuilder Configure(this IStandardHedgingHandlerBuilder builder, IConfigurationSection section)
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNull(section);

        if (!section.GetChildren().Any())
        {
            Throw.ArgumentNullException(nameof(section));
        }

        _ = builder.Services.Configure<HttpStandardHedgingResilienceOptions>(builder.Name, section, o => o.ErrorOnUnknownConfiguration = true);

        return builder;
    }

    /// <summary>
    /// Configures the <see cref="HttpStandardResilienceOptions"/> for the standard hedging pipeline.
    /// </summary>
    /// <param name="builder">The pipeline builder.</param>
    /// <param name="configure">The configure method.</param>
    /// <returns>The value of <paramref name="builder"/>.</returns>
    public static IStandardHedgingHandlerBuilder Configure(this IStandardHedgingHandlerBuilder builder, Action<HttpStandardHedgingResilienceOptions> configure)
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNull(configure);

        return builder.Configure((options, _) => configure(options));
    }

    /// <summary>
    /// Configures the <see cref="HttpStandardResilienceOptions"/> for the standard hedging pipeline.
    /// </summary>
    /// <param name="builder">The pipeline builder.</param>
    /// <param name="configure">The configure method.</param>
    /// <returns>The value of <paramref name="builder"/>.</returns>
    public static IStandardHedgingHandlerBuilder Configure(this IStandardHedgingHandlerBuilder builder, Action<HttpStandardHedgingResilienceOptions, IServiceProvider> configure)
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNull(configure);

        _ = builder.Services.AddOptions<HttpStandardHedgingResilienceOptions>(builder.Name).Configure(configure);

        return builder;
    }

    /// <summary>
    /// Instructs the underlying pipeline builder to select the pipeline instance by redacted authority (scheme + host + port).
    /// </summary>
    /// <param name="builder">The builder instance.</param>
    /// <returns>The value of <paramref name="builder"/>.</returns>
    public static IStandardHedgingHandlerBuilder SelectPipelineByAuthority(this IStandardHedgingHandlerBuilder builder)
    {
        _ = Throw.IfNull(builder);

        var pipelineName = PipelineNameHelper.GetName(builder.Name, HedgingConstants.InnerHandlerPostfix);

        PipelineKeyProviderHelper.SelectPipelineByAuthority(builder.Services, pipelineName);

        return builder;
    }

    /// <summary>
    /// Instructs the underlying pipeline builder to select the pipeline instance by custom selector.
    /// </summary>
    /// <param name="builder">The builder instance.</param>
    /// <param name="selectorFactory">The factory that returns key selector.</param>
    /// <returns>The value of <paramref name="builder"/>.</returns>
    /// <remarks>The pipeline key is used in metrics and logs, so don't return any sensitive values.</remarks>
    public static IStandardHedgingHandlerBuilder SelectPipelineBy(this IStandardHedgingHandlerBuilder builder, Func<IServiceProvider, Func<HttpRequestMessage, string>> selectorFactory)
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNull(selectorFactory);

        var pipelineName = PipelineNameHelper.GetName(builder.Name, HedgingConstants.InnerHandlerPostfix);

        PipelineKeyProviderHelper.SelectPipelineBy(builder.Services, pipelineName, selectorFactory);

        return builder;
    }
}
