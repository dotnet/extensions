// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Resilience.FaultInjection;

namespace Microsoft.Extensions.Http.Resilience.FaultInjection.PerformanceTests;

public class FaultInjectionRequestBenchmarks
{
    private const string HttpClientIdentifier = "HttpClientClass";
    private static readonly Uri _defaultUrl = new("https://www.google.ca/");
    private ServiceProvider _serviceProvider = null!;
    private HttpClient _client = null!;

    [GlobalSetup]
    public void GlobalSetup()
    {
        var services = new ServiceCollection();

        if (SetupFaultInjection)
        {
            Action<HttpFaultInjectionOptionsBuilder> action = builder =>
            builder
                .Configure(option =>
                {
                    option.ChaosPolicyOptionsGroups = new Dictionary<string, ChaosPolicyOptionsGroup>();
                });

            services.AddHttpClientFaultInjection(action);
        }

        services.AddHttpClient(HttpClientIdentifier);

        _serviceProvider = services.BuildServiceProvider();
        var clientFactory = _serviceProvider.GetRequiredService<IHttpClientFactory>();
        _client = clientFactory.CreateClient(HttpClientIdentifier);
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        _serviceProvider.Dispose();
    }

    [Params(
        true,
        false)]
    public bool SetupFaultInjection { get; set; }

    [Benchmark]
    public async Task SendAsync()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, _defaultUrl);
        await _client.SendAsync(request, HttpCompletionOption.ResponseContentRead, CancellationToken.None).ConfigureAwait(false);
    }
}
