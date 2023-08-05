// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Http.Resilience.Bench;

public class HedgingBenchmark
{
    private static readonly Uri _uri = new(HttpClientFactory.PrimaryEndpoint);
    private static HttpRequestMessage Request => new(HttpMethod.Post, _uri);

    private HttpClient _client = null!;

    [GlobalSetup]
    public void GlobalSetup()
    {
        var serviceProvider = HttpClientFactory.InitializeServiceProvider(Type);
        var factory = serviceProvider.GetRequiredService<IHttpClientFactory>();
#pragma warning disable EA0006 // Replace uses of 'Enum.GetName' and 'Enum.ToString' with the '[EnumStrings]' code generator for improved performance
        _client = factory.CreateClient(Type.ToString());
#pragma warning restore EA0006 // Replace uses of 'Enum.GetName' and 'Enum.ToString' with the '[EnumStrings]' code generator for improved performance
    }

    [Params(
        HedgingClientType.NoRoutes,
        HedgingClientType.Weighted,
        HedgingClientType.Ordered,
        HedgingClientType.Weighted | HedgingClientType.ManyRoutes,
        HedgingClientType.Ordered | HedgingClientType.ManyRoutes)]
    public HedgingClientType Type { get; set; }

    [Benchmark]
    public Task<HttpResponseMessage> HedgingCall() => _client.SendAsync(Request, CancellationToken.None);
}
