// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience.Internal;
using Microsoft.Extensions.Resilience;
using Microsoft.Shared.Diagnostics;
using Polly;

namespace Microsoft.Extensions.Http.Resilience;

public static partial class HttpClientBuilderExtensions
{
    /// <summary>
    /// Adds a <see cref="PolicyHttpMessageHandler" /> that uses a named inline resilience pipeline configured by returned <see cref="IHttpResiliencePipelineBuilder"/>.
    /// </summary>
    /// <param name="builder">The builder instance.</param>
    /// <param name="pipelineIdentifier">The custom identifier for the pipeline, used in the name of the pipeline.</param>
    /// <returns>The HTTP pipeline builder instance.</returns>
    /// <remarks>
    /// The final pipeline name is combination of <see cref="IHttpClientBuilder.Name"/> and <paramref name="pipelineIdentifier"/>.
    /// Use pipeline identifier if your HTTP client contains multiple resilience handlers.
    /// </remarks>
    public static IHttpResiliencePipelineBuilder AddResilienceHandler(this IHttpClientBuilder builder, string pipelineIdentifier)
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNullOrEmpty(pipelineIdentifier);

        var pipelineBuilder = builder.AddHttpResiliencePipeline(pipelineIdentifier);

        _ = builder.AddHttpMessageHandler(serviceProvider =>
        {
            var selector = CreatePipelineSelector(serviceProvider, pipelineBuilder.PipelineName);
            return new ResilienceHandler(pipelineBuilder.PipelineName, selector);
        });

        return pipelineBuilder;
    }

    private static Func<HttpRequestMessage, IAsyncPolicy<HttpResponseMessage>> CreatePipelineSelector(IServiceProvider serviceProvider, string pipelineName)
    {
        var resilienceProvider = serviceProvider.GetRequiredService<IResiliencePipelineProvider>();
        var pipelineKeyProvider = serviceProvider.GetPipelineKeyProvider(pipelineName);

        if (pipelineKeyProvider == null)
        {
            var pipeline = resilienceProvider.GetPipeline<HttpResponseMessage>(pipelineName);
            return _ => pipeline;
        }
        else
        {
            TouchPipelineKey(pipelineKeyProvider);

            return request =>
            {
                var pipelineKey = pipelineKeyProvider.GetPipelineKey(request);
                return resilienceProvider.GetPipeline<HttpResponseMessage>(pipelineName, pipelineKey);
            };
        }
    }

    private static void TouchPipelineKey(IPipelineKeyProvider provider)
    {
        // this piece of code eagerly checks that the pipeline key provider is correctly configured
        // combined with HttpClient auto-activation we can detect any issues on startup
        if (provider is ByAuthorityPipelineKeyProvider)
        {
#pragma warning disable S1075 // URIs should not be hardcoded - this URL is not used for any real request, nor in any telemetry
            using var request = new HttpRequestMessage(HttpMethod.Get, "https://localhost:123");
#pragma warning restore S1075 // URIs should not be hardcoded
            _ = provider.GetPipelineKey(request);
        }
    }

    private static HttpResiliencePipelineBuilder AddHttpResiliencePipeline(this IHttpClientBuilder builder, string pipelineIdentifier)
    {
        _ = builder.Services.ConfigureHttpFailureResultContext();
        var pipelineName = PipelineNameHelper.GetPipelineName(builder.Name, pipelineIdentifier);
        var pipelineBuilder = builder.Services.AddResiliencePipeline<HttpResponseMessage>(pipelineName);

        return new HttpResiliencePipelineBuilder(pipelineBuilder);
    }
}
