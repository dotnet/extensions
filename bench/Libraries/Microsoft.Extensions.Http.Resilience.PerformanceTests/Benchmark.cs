// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Http.Resilience.Bench;

public class Benchmark
{
    private static HttpRequestMessage Request => new(HttpMethod.Post, "https://bogus");

    private System.Net.Http.HttpClient _client = null!;

    [GlobalSetup]
    public void GlobalSetup()
    {
        var type = RoutingStrategy;

        if (ManyRoutes)
        {
            type |= HedgingClientType.ManyRoutes;
        }

        var serviceProvider = HttpClientFactory.InitializeServiceProvider(type);
        var factory = serviceProvider.GetRequiredService<IHttpClientFactory>();
#pragma warning disable R9A033 // Replace uses of 'Enum.GetName' and 'Enum.ToString' with the '[EnumStrings]' code generator for improved performance
        _client = factory.CreateClient(type.ToString());
#pragma warning restore R9A033 // Replace uses of 'Enum.GetName' and 'Enum.ToString' with the '[EnumStrings]' code generator for improved performance
    }

    [Params(HedgingClientType.Weighted, HedgingClientType.Ordered)]
    public HedgingClientType RoutingStrategy { get; set; }

    [Params(true, false)]
    public bool ManyRoutes { get; set; }

    [Benchmark]
    public Task<HttpResponseMessage> HedgingCall()
    {
        return _client.SendAsync(Request, CancellationToken.None);
    }
}
