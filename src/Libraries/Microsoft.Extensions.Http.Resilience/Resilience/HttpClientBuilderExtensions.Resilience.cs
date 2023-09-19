// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.ExceptionSummarization;
using Microsoft.Extensions.Http.Resilience.Internal;
using Microsoft.Extensions.Resilience;
using Microsoft.Shared.Diagnostics;
using Microsoft.Shared.Text;
using Polly;
using Polly.Registry;

namespace Microsoft.Extensions.Http.Resilience;

/// <summary>
/// Extensions for <see cref="IHttpClientBuilder"/>.
/// </summary>
public static partial class HttpClientBuilderExtensions
{
    /// <summary>
    /// Adds a resilience pipeline handler that uses a named inline resilience pipeline.
    /// </summary>
    /// <param name="builder">The builder instance.</param>
    /// <param name="pipelineName">The custom identifier for the resilience pipeline, used in the name of the pipeline.</param>
    /// <param name="configure">The callback that configures the pipeline.</param>
    /// <returns>The HTTP pipeline builder instance.</returns>
    /// <remarks>
    /// The final pipeline name is combination of <see cref="IHttpClientBuilder.Name"/> and <paramref name="pipelineName"/>.
    /// Use pipeline name identifier if your HTTP client contains multiple resilience handlers.
    /// </remarks>
    public static IHttpResiliencePipelineBuilder AddResilienceHandler(
        this IHttpClientBuilder builder,
        string pipelineName,
        Action<ResiliencePipelineBuilder<HttpResponseMessage>> configure)
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNullOrEmpty(pipelineName);
        _ = Throw.IfNull(configure);

        return builder.AddResilienceHandler(pipelineName, ConfigureBuilder);

        void ConfigureBuilder(ResiliencePipelineBuilder<HttpResponseMessage> builder, ResilienceHandlerContext context) => configure(builder);
    }

    /// <summary>
    /// Adds a resilience pipeline handler that uses a named inline resilience pipeline.
    /// </summary>
    /// <param name="builder">The builder instance.</param>
    /// <param name="pipelineName">The custom identifier for the resilience pipeline, used in the name of the pipeline.</param>
    /// <param name="configure">The callback that configures the pipeline.</param>
    /// <returns>The HTTP pipeline builder instance.</returns>
    /// <remarks>
    /// The final pipeline name is combination of <see cref="IHttpClientBuilder.Name"/> and <paramref name="pipelineName"/>.
    /// Use pipeline name identifier if your HTTP client contains multiple resilience handlers.
    /// </remarks>
    public static IHttpResiliencePipelineBuilder AddResilienceHandler(
        this IHttpClientBuilder builder,
        string pipelineName,
        Action<ResiliencePipelineBuilder<HttpResponseMessage>, ResilienceHandlerContext> configure)
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNullOrEmpty(pipelineName);
        _ = Throw.IfNull(configure);

        var pipelineBuilder = builder.AddHttpResiliencePipeline(pipelineName, configure);

        _ = builder.AddHttpMessageHandler(serviceProvider =>
        {
            var selector = CreatePipelineSelector(serviceProvider, pipelineBuilder.PipelineName);
            var provider = serviceProvider.GetRequiredService<ResiliencePipelineProvider<HttpKey>>();

            return new ResilienceHandler(selector);
        });

        return pipelineBuilder;
    }

    private static Func<HttpRequestMessage, ResiliencePipeline<HttpResponseMessage>> CreatePipelineSelector(IServiceProvider serviceProvider, string pipelineName)
    {
        var resilienceProvider = serviceProvider.GetRequiredService<ResiliencePipelineProvider<HttpKey>>();
        var pipelineKeyProvider = serviceProvider.GetPipelineKeyProvider(pipelineName);

        if (pipelineKeyProvider == null)
        {
            var pipeline = resilienceProvider.GetPipeline<HttpResponseMessage>(new HttpKey(pipelineName, string.Empty));
            return _ => pipeline;
        }
        else
        {
            TouchPipelineKey(pipelineKeyProvider);

            return request =>
            {
                var key = pipelineKeyProvider(request);
                return resilienceProvider.GetPipeline<HttpResponseMessage>(new HttpKey(pipelineName, key));
            };
        }
    }

    private static void TouchPipelineKey(Func<HttpRequestMessage, string> provider)
    {
        // this piece of code eagerly checks that the pipeline key provider is correctly configured
        // combined with HttpClient auto-activation we can detect any issues on startup
#pragma warning disable S1075 // URIs should not be hardcoded - this URL is not used for any real request, nor in any telemetry
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://localhost:123");
#pragma warning restore S1075 // URIs should not be hardcoded
        _ = provider(request);
    }

    private static HttpResiliencePipelineBuilder AddHttpResiliencePipeline(
        this IHttpClientBuilder builder,
        string name,
        Action<ResiliencePipelineBuilder<HttpResponseMessage>, ResilienceHandlerContext> configure)
    {
        var pipelineName = PipelineNameHelper.GetName(builder.Name, name);
        var key = new HttpKey(pipelineName, string.Empty);

        _ = builder.Services.AddResiliencePipeline<HttpKey, HttpResponseMessage>(key, (builder, context) => configure(builder, new ResilienceHandlerContext(context)));

        ConfigureHttpServices(builder.Services);

        return new(pipelineName, builder.Services);
    }

    private static void ConfigureHttpServices(IServiceCollection services)
    {
        // don't add any new service if this method is called multiple times
        if (services.Contains(Marker.ServiceDescriptor))
        {
            return;
        }

        services.Add(Marker.ServiceDescriptor);

        // This code configure the multi-instance support of the registry
        _ = services.Configure<ResiliencePipelineRegistryOptions<HttpKey>>(options =>
        {
            options.BuilderNameFormatter = key => key.Name;
            options.InstanceNameFormatter = key => key.InstanceName;
            options.BuilderComparer = HttpKey.BuilderComparer;
        });

        _ = services
            .AddExceptionSummarizer(b => b.AddHttpProvider())
            .ConfigureFailureResultContext<HttpResponseMessage>((response) =>
            {
                if (response != null)
                {
                    return FailureResultContext.Create(
                        failureReason: ((int)response.StatusCode).ToInvariantString(),
                        additionalInformation: response.StatusCode.ToInvariantString());
                }

                return FailureResultContext.Create();
            });
    }

    private sealed class Marker
    {
        public static readonly ServiceDescriptor ServiceDescriptor = ServiceDescriptor.Singleton<Marker, Marker>();
    }

    private record HttpResiliencePipelineBuilder(string PipelineName, IServiceCollection Services) : IHttpResiliencePipelineBuilder;
}
