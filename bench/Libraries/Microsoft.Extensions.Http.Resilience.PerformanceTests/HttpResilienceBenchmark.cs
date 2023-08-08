// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Http.Resilience.Bench;

public class HttpResilienceBenchmark
{
    private static readonly Uri _uri = new(HttpClientFactory.PrimaryEndpoint);

    private HttpClient _client = null!;
    private HttpClient _standardClient = null!;
    private HttpClient _singleHandlerClient = null!;
    private HttpClient _hedgingClientOrdered = null!;
    private HttpClient _hedgingClientNoRoutes = null!;

    private static HttpRequestMessage Request
    {
        get
        {
            var request = new HttpRequestMessage(HttpMethod.Post, _uri);
            request.Options.Set(new HttpRequestOptionsKey<string>("dummy"), "dummy");
            return request;
        }
    }

    [GlobalSetup]
    public void GlobalSetup()
    {
        var serviceProvider = HttpClientFactory.InitializeServiceProvider(HedgingClientType.Ordered, HedgingClientType.NoRoutes);
        var factory = serviceProvider.GetRequiredService<IHttpClientFactory>();
        _client = factory.CreateClient(HttpClientFactory.EmptyClient);
        _standardClient = factory.CreateClient(HttpClientFactory.StandardClient);
        _singleHandlerClient = factory.CreateClient(HttpClientFactory.SingleHandlerClient);
        _hedgingClientNoRoutes = factory.CreateClient(nameof(HedgingClientType.NoRoutes));
        _hedgingClientOrdered = factory.CreateClient(nameof(HedgingClientType.Ordered));
    }

    [Benchmark(Baseline = true)]
    public Task<HttpResponseMessage> DefaultClient()
    {
        return _client.SendAsync(Request, CancellationToken.None);
    }

    [Benchmark]
    public Task<HttpResponseMessage> SingleHandler()
    {
        return _singleHandlerClient.SendAsync(Request, CancellationToken.None);
    }

    [Benchmark]
    public Task<HttpResponseMessage> StandardResilienceHandler()
    {
        return _standardClient.SendAsync(Request, CancellationToken.None);
    }

    [Benchmark]
    public Task<HttpResponseMessage> StandardHedgingHandler_RoutesFromRequest()
    {
        return _hedgingClientNoRoutes.SendAsync(Request, CancellationToken.None);
    }

    [Benchmark]
    public Task<HttpResponseMessage> StandardHedgingHandler_RoutesFromConfig()
    {
        return _hedgingClientOrdered.SendAsync(Request, CancellationToken.None);
    }
}
