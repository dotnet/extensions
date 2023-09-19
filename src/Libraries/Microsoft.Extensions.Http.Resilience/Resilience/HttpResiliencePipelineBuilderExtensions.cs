// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net.Http;
using Microsoft.Extensions.Compliance.Classification;
using Microsoft.Extensions.Compliance.Redaction;
using Microsoft.Extensions.Http.Resilience.Internal;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Http.Resilience;

/// <summary>
/// Extensions for <see cref="IHttpResiliencePipelineBuilder"/>.
/// </summary>
public static class HttpResiliencePipelineBuilderExtensions
{
    /// <summary>
    /// Instructs the underlying builder to select the pipeline instance by redacted authority (scheme + host + port).
    /// </summary>
    /// <param name="builder">The builder instance.</param>
    /// <param name="classification">The data class associated with the authority.</param>
    /// <returns>The same builder instance.</returns>
    /// <remarks>The authority is redacted using <see cref="Redactor"/> retrieved for <paramref name="classification"/>.</remarks>
    public static IHttpResiliencePipelineBuilder SelectPipelineByAuthority(this IHttpResiliencePipelineBuilder builder, DataClassification classification)
    {
        _ = Throw.IfNull(builder);

        PipelineKeyProviderHelper.SelectPipelineByAuthority(builder.Services, builder.PipelineName, classification);

        return builder;
    }

    /// <summary>
    /// Instructs the underlying builder to select the pipeline instance by custom selector.
    /// </summary>
    /// <param name="builder">The builder instance.</param>
    /// <param name="selectorFactory">The factory that returns a key selector.</param>
    /// <returns>The same builder instance.</returns>
    /// <remarks>The pipeline key is used in metrics and logs, so don't return any sensitive values.</remarks>
    public static IHttpResiliencePipelineBuilder SelectPipelineBy(this IHttpResiliencePipelineBuilder builder, Func<IServiceProvider, Func<HttpRequestMessage, string>> selectorFactory)
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNull(selectorFactory);

        PipelineKeyProviderHelper.SelectPipelineBy(builder.Services, builder.PipelineName, selectorFactory);

        return builder;
    }
}
