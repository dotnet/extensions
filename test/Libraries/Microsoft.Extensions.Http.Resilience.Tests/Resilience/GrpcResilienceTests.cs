// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if !NETFRAMEWORK

using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Grpc.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Http.Resilience.Test.Grpc;
using Microsoft.Extensions.Http.Resilience.Test.Helpers;
using Polly;
using Xunit;

namespace Microsoft.Extensions.Http.Resilience.Test.Resilience;

public class GrpcResilienceTests
{
    private IHost _host;
    private HttpMessageHandler _handler;

    public GrpcResilienceTests()
    {
        _host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                    .UseTestServer()
                    .ConfigureServices(services => services.AddGrpc())
                    .Configure(builder =>
                    {
                        builder.UseRouting();
                        builder.UseEndpoints(endpoints => endpoints.MapGrpcService<GreeterService>());
                    });
            })
            .Build();

        _host.Start();

        _handler = _host.GetTestServer().CreateHandler();
    }

    [Theory]
    [CombinatorialData]
    public async Task SayHello_NoResilience_OK(bool asynchronous)
    {
        var response = await SendRequest(CreateClient(), asynchronous);

        response.Message.Should().Be("HI!");
    }

    [Theory]
    [CombinatorialData]
    public async Task SayHello_StandardResilience_OK(bool asynchronous)
    {
        var client = CreateClient(builder => builder.AddStandardResilienceHandler());
        var response = await SendRequest(client, asynchronous);

        response.Message.Should().Be("HI!");
    }

    [Theory]
    [CombinatorialData]
    public async Task SayHello_StandardHedging_OK(bool asynchronous)
    {
        var client = CreateClient(builder => builder.AddStandardHedgingHandler());
        var response = await SendRequest(client, asynchronous);

        response.Message.Should().Be("HI!");
    }

    [Theory]
    [CombinatorialData]
    public async Task SayHello_CustomResilience_OK(bool asynchronous)
    {
        var client = CreateClient(builder => builder.AddResilienceHandler("custom", builder => builder.AddTimeout(TimeSpan.FromSeconds(1))));
        var response = await SendRequest(client, asynchronous);

        response.Message.Should().Be("HI!");
    }

    [Theory]
    [CombinatorialData]
    public async Task SayHello_StandardResilience_CancelledWhileAttemptCompletes_Cancelled(bool asynchronous)
    {
        using var cts = new CancellationTokenSource();

        // The caller cancels while the attempt is in flight, and the attempt then completes
        // with a transient failure that the retry strategy is configured to handle.
        var client = CreateClient(
            builder => builder.AddStandardResilienceHandler(),
            new TestHandlerStub((_, _) =>
            {
                cts.Cancel();
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable) { Version = HttpVersion.Version20 });
            }));

        var act = () => SendRequest(client, asynchronous, cts.Token);

        (await act.Should().ThrowAsync<RpcException>()).Which.StatusCode.Should().Be(StatusCode.Cancelled);
    }

    private static Task<HelloReply> SendRequest(Greeter.GreeterClient client, bool asynchronous, CancellationToken cancellationToken)
    {
        var request = new HelloRequest { Name = "dummy" };

        if (asynchronous)
        {
            return client.SayHelloAsync(request, cancellationToken: cancellationToken).ResponseAsync;
        }
        else
        {
            return Task.FromResult(client.SayHello(request, cancellationToken: cancellationToken));
        }
    }

    private static Task<HelloReply> SendRequest(Greeter.GreeterClient client, bool asynchronous)
    {
        var request = new HelloRequest { Name = "dummy" };

        if (asynchronous)
        {
            return client.SayHelloAsync(request).ResponseAsync;
        }
        else
        {
            return Task.FromResult(client.SayHello(request));
        }
    }

    private Greeter.GreeterClient CreateClient(Action<IHttpClientBuilder>? configure = null, HttpMessageHandler? primaryHandler = null)
    {
        var services = new ServiceCollection();
        var clientBuilder = services
            .AddGrpcClient<Greeter.GreeterClient>(options =>
            {
                options.Address = _host.GetTestServer().BaseAddress;
            })
            .ConfigurePrimaryHttpMessageHandler(() => primaryHandler ?? _handler);

        configure?.Invoke(clientBuilder);

        return services.BuildServiceProvider().GetRequiredService<Greeter.GreeterClient>();

    }

    public class GreeterService : Greeter.GreeterBase
    {
        public override Task<HelloReply> SayHello(HelloRequest request, ServerCallContext context)
        {
            return Task.FromResult(new HelloReply { Message = "HI!" });
        }
    }
}
#endif
