// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Compliance.Classification;
using Microsoft.Extensions.Compliance.Redaction;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience.Internal;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Http.Resilience;

/// <summary>
/// Extensions for <see cref="IHttpStandardResiliencePipelineBuilder"/>.
/// </summary>
public static class HttpStandardResiliencePipelineBuilderExtensions
{
    /// <summary>
    /// Configures the <see cref="HttpStandardResilienceOptions"/> for the standard pipeline.
    /// </summary>
    /// <param name="builder">The pipeline builder.</param>
    /// <param name="section">The section that the options will bind against.</param>
    /// <returns>The same builder instance.</returns>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(HttpStandardResilienceOptions))]
    public static IHttpStandardResiliencePipelineBuilder Configure(this IHttpStandardResiliencePipelineBuilder builder, IConfigurationSection section)
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNull(section);
        var options = Throw.IfNull(section.Get<HttpStandardResilienceOptions>());

        _ = builder.Services.Configure<HttpStandardResilienceOptions>(
            builder.PipelineName,
            section,
#if NET6_0_OR_GREATER
            o => o.ErrorOnUnknownConfiguration = true);
#else
            _ => { });
#endif
        return builder;
    }

    /// <summary>
    /// Configures the <see cref="HttpStandardResilienceOptions"/> for the standard pipeline.
    /// </summary>
    /// <param name="builder">The pipeline builder.</param>
    /// <param name="configure">The configure method.</param>
    /// <returns>The same builder instance.</returns>
#pragma warning disable S3872 // Parameter names should not duplicate the names of their methods
    public static IHttpStandardResiliencePipelineBuilder Configure(this IHttpStandardResiliencePipelineBuilder builder, Action<HttpStandardResilienceOptions> configure)
#pragma warning restore S3872 // Parameter names should not duplicate the names of their methods
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNull(configure);

        return builder.Configure((options, _) => configure(options));
    }

    /// <summary>
    /// Configures the <see cref="HttpStandardResilienceOptions"/> for the standard pipeline.
    /// </summary>
    /// <param name="builder">The pipeline builder.</param>
    /// <param name="configure">The configure method.</param>
    /// <returns>The same builder instance.</returns>
#pragma warning disable S3872 // Parameter names should not duplicate the names of their methods
    [Experimental]
    public static IHttpStandardResiliencePipelineBuilder Configure(this IHttpStandardResiliencePipelineBuilder builder, Action<HttpStandardResilienceOptions, IServiceProvider> configure)
#pragma warning restore S3872 // Parameter names should not duplicate the names of their methods
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNull(configure);

        _ = builder.Services.AddOptions<HttpStandardResilienceOptions>(builder.PipelineName).Configure(configure);

        return builder;
    }

    /// <summary>
    /// Instructs the underlying pipeline builder to select the pipeline instance by redacted authority (scheme + host + port).
    /// </summary>
    /// <param name="builder">The builder instance.</param>
    /// <param name="classification">The data class associated with the authority.</param>
    /// <returns>The same builder instance.</returns>
    /// <remarks>The authority is redacted using <see cref="Redactor"/> retrieved for <paramref name="classification"/>.</remarks>
    public static IHttpStandardResiliencePipelineBuilder SelectPipelineByAuthority(this IHttpStandardResiliencePipelineBuilder builder, DataClassification classification)
    {
        _ = Throw.IfNull(builder);

        PipelineKeyProviderHelper.SelectPipelineByAuthority(builder.Services, builder.PipelineName, classification);

        return builder;
    }

    /// <summary>
    /// Instructs the underlying pipeline builder to select the pipeline instance by custom selector.
    /// </summary>
    /// <param name="builder">The builder instance.</param>
    /// <param name="selectorFactory">The factory that returns <see cref="PipelineKeySelector"/> selector.</param>
    /// <returns>The same builder instance.</returns>
    /// <remarks>The pipeline key is used in metrics and logs, do not return any sensitive value.</remarks>
    public static IHttpStandardResiliencePipelineBuilder SelectPipelineBy(this IHttpStandardResiliencePipelineBuilder builder, Func<IServiceProvider, PipelineKeySelector> selectorFactory)
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNull(selectorFactory);

        PipelineKeyProviderHelper.SelectPipelineBy(builder.Services, builder.PipelineName, selectorFactory);

        return builder;
    }
}
