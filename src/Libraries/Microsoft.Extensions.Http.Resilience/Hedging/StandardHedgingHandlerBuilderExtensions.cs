// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Compliance.Classification;
using Microsoft.Extensions.Compliance.Redaction;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Http.Resilience;

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
#if NET6_0_OR_GREATER
            o => o.ErrorOnUnknownConfiguration = true);
#else
            _ => { });
#endif

        return builder;
    }

    /// <summary>
    /// Configures the <see cref="HttpStandardResilienceOptions"/> for the standard hedging pipeline.
    /// </summary>
    /// <param name="builder">The pipeline builder.</param>
    /// <param name="configure">The configure method.</param>
    /// <returns>The same builder instance.</returns>
#pragma warning disable S3872 // Parameter names should not duplicate the names of their methods
    public static IStandardHedgingHandlerBuilder Configure(this IStandardHedgingHandlerBuilder builder, Action<HttpStandardHedgingResilienceOptions> configure)
#pragma warning restore S3872 // Parameter names should not duplicate the names of their methods
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
    /// <returns>The same builder instance.</returns>
#pragma warning disable S3872 // Parameter names should not duplicate the names of their methods
    [Experimental]
    public static IStandardHedgingHandlerBuilder Configure(this IStandardHedgingHandlerBuilder builder, Action<HttpStandardHedgingResilienceOptions, IServiceProvider> configure)
#pragma warning restore S3872 // Parameter names should not duplicate the names of their methods
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
    /// <param name="classification">The data class associated with the authority.</param>
    /// <returns>The same builder instance.</returns>
    /// <remarks>The authority is redacted using <see cref="Redactor"/> retrieved for <paramref name="classification"/>.</remarks>
    public static IStandardHedgingHandlerBuilder SelectPipelineByAuthority(this IStandardHedgingHandlerBuilder builder, DataClassification classification)
    {
        _ = Throw.IfNull(builder);

        _ = builder.EndpointResiliencePipelineBuilder.SelectPipelineByAuthority(classification);

        return builder;
    }

    /// <summary>
    /// Instructs the underlying pipeline builder to select the pipeline instance by custom selector.
    /// </summary>
    /// <param name="builder">The builder instance.</param>
    /// <param name="selectorFactory">The factory that returns <see cref="PipelineKeySelector"/> selector.</param>
    /// <returns>The same builder instance.</returns>
    /// <remarks>The pipeline key is used in metrics and logs, do not return any sensitive value.</remarks>
    public static IStandardHedgingHandlerBuilder SelectPipelineBy(this IStandardHedgingHandlerBuilder builder, Func<IServiceProvider, PipelineKeySelector> selectorFactory)
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNull(selectorFactory);

        _ = builder.EndpointResiliencePipelineBuilder.SelectPipelineBy(selectorFactory);

        return builder;
    }
}
