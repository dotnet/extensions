// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Resilience.FaultInjection;
using Polly;
using Xunit;

namespace Microsoft.Extensions.Http.Resilience.FaultInjection.Internal.Test;

public class FaultInjectionWeightAssignmentContextMessageHandlerTest
{
    [Fact]
    public async Task SendAsync_WithContext()
    {
        var services = new ServiceCollection();
        var httpClientIdentifier = "HttpClientClass";

        services
            .AddHttpClient<HttpClientClass>()
            .AddHttpMessageHandler(services =>
            {
                var weightAssignmentsOptions = services.GetRequiredService<IOptionsMonitor<FaultPolicyWeightAssignmentsOptions>>();
                return ActivatorUtilities.CreateInstance<FaultInjectionWeightAssignmentContextMessageHandler>(services, httpClientIdentifier, weightAssignmentsOptions);
            })
            .AddHttpMessageHandler(() => new TestMessageHandler());

        var clientFactory = services
            .BuildServiceProvider()
            .GetRequiredService<IHttpClientFactory>();
        var httpClient = clientFactory.CreateClient(httpClientIdentifier);

        using var request = new HttpRequestMessage(HttpMethod.Get, "https://localhost:12345");
        var context = new Context();
        request.SetPolicyExecutionContext(context);
        var response = await httpClient.SendAsync(request);

        Assert.True(response.IsSuccessStatusCode);
    }

    [Fact]
    public async Task SendAsync_WithoutContext()
    {
        var services = new ServiceCollection();
        var httpClientIdentifier = "HttpClientClass";

        services
            .AddHttpClient<HttpClientClass>()
            .AddHttpMessageHandler(services =>
            {
                var weightAssignmentsOptions = services.GetRequiredService<IOptionsMonitor<FaultPolicyWeightAssignmentsOptions>>();
                return ActivatorUtilities.CreateInstance<FaultInjectionWeightAssignmentContextMessageHandler>(services, httpClientIdentifier, weightAssignmentsOptions);
            })
            .AddHttpMessageHandler(() => new TestMessageHandler());

        var clientFactory = services
            .BuildServiceProvider()
            .GetRequiredService<IHttpClientFactory>();
        var httpClient = clientFactory.CreateClient(httpClientIdentifier);

        using var request = new HttpRequestMessage(HttpMethod.Get, "https://localhost:12345");
        var response = await httpClient.SendAsync(request);

        Assert.True(response.IsSuccessStatusCode);
    }

    private class HttpClientClass
    {
    }

    private class TestMessageHandler : DelegatingHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = new StringContent("OK") });
        }
    }
}
