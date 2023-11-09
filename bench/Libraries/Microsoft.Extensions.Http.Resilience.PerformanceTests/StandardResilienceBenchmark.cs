// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Polly;
using Polly.Timeout;

namespace Microsoft.Extensions.Http.Resilience.PerformanceTests;

public class StandardResilienceBenchmark
{
    private static readonly Uri _uri = new(HttpClientFactory.PrimaryEndpoint);

    private HttpClient _v7 = null!;
    private HttpClient _v8 = null!;
    private CancellationToken _cancellationToken;

    private static HttpRequestMessage Request => new(HttpMethod.Post, _uri);

    [GlobalSetup]
    public void Prepare()
    {
        _cancellationToken = new CancellationTokenSource().Token;

        var services = new ServiceCollection();

        // The ResiliencePipelineBuilder added by Polly includes telemetry, which affects the results.
        // Since Polly v7 does not include telemetry either, let's disable the telemetry for v8 for fair results.
        services.TryAddTransient<ResiliencePipelineBuilder>();

        services
            .AddHttpClient("v8")
            .ConfigurePrimaryHttpMessageHandler(() => new NoRemoteCallHandler())
            .AddStandardResilienceHandler();

        var builder = Policy.Handle<HttpRequestException>().Or<TimeoutRejectedException>().OrResult<HttpResponseMessage>(r =>
        {
            var statusCode = (int)r.StatusCode;

            return statusCode >= 500 ||
                r.StatusCode == HttpStatusCode.RequestTimeout ||
                statusCode == 429;
        });

        var policy = Policy.WrapAsync(
            Policy.TimeoutAsync<HttpResponseMessage>(3),
            builder.AdvancedCircuitBreakerAsync(0.1, TimeSpan.FromSeconds(30), 100, TimeSpan.FromSeconds(5)),
            builder.WaitAndRetryAsync(3, _ => TimeSpan.FromSeconds(1)),
            Policy.BulkheadAsync<HttpResponseMessage>(1000, 100),
            Policy.TimeoutAsync<HttpResponseMessage>(30));

        services
            .AddHttpClient("v7")
            .ConfigurePrimaryHttpMessageHandler(() => new NoRemoteCallHandler())
            .AddPolicyHandler(_ => policy);

        var factory = services.BuildServiceProvider().GetRequiredService<IHttpClientFactory>();

        _v7 = factory.CreateClient("v7");
        _v8 = factory.CreateClient("v8");
    }

    [Benchmark(Baseline = true)]
    public Task<HttpResponseMessage> StandardPipeline_Polly_V7()
    {
        return _v7!.SendAsync(Request, _cancellationToken);
    }

    [Benchmark]
    public Task<HttpResponseMessage> StandardPipeline_Polly_V8()
    {
        return _v8!.SendAsync(Request, _cancellationToken);
    }
}
