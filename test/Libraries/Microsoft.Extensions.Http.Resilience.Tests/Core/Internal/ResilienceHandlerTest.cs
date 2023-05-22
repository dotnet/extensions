// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Http.Resilience.Internal;
using Microsoft.Extensions.Http.Telemetry;
using Microsoft.Extensions.Telemetry;
using Polly;
using Xunit;

namespace Microsoft.Extensions.Http.Resilience.Test.Internals;

public class ResilienceHandlerTest
{
    [InlineData(true)]
    [InlineData(false)]
    [Theory]
    public async Task SendAsync_EnsureRequestMetadataFlows(bool executionContextSet)
    {
        using var handler = new ResilienceHandler("dummy", _ => Policy.NoOpAsync<HttpResponseMessage>());
        using var invoker = new HttpMessageInvoker(handler);
        using var request = new HttpRequestMessage();

        if (executionContextSet)
        {
            request.SetPolicyExecutionContext(new Context());
        }

        request.SetRequestMetadata(new RequestMetadata());

        handler.InnerHandler = new TestHandlerStub(HttpStatusCode.OK);

        await invoker.SendAsync(request, default);

        if (executionContextSet)
        {
            Assert.NotNull(request.GetPolicyExecutionContext()![TelemetryConstants.RequestMetadataKey]);
        }
        else
        {
            Assert.Null(request.GetPolicyExecutionContext());
        }
    }

    [InlineData(true)]
    [InlineData(false)]
    [Theory]
    public async Task SendAsync_EnsureExecutionContext(bool executionContextSet)
    {
        using var handler = new ResilienceHandler("dummy", _ => Policy.NoOpAsync<HttpResponseMessage>());
        using var invoker = new HttpMessageInvoker(handler);
        using var request = new HttpRequestMessage();

        if (executionContextSet)
        {
            request.SetPolicyExecutionContext(new Context());
        }

        handler.InnerHandler = new TestHandlerStub(HttpStatusCode.OK);

        await invoker.SendAsync(request, default);

        if (executionContextSet)
        {
            Assert.NotNull(request.GetPolicyExecutionContext());
        }
        else
        {
            Assert.Null(request.GetPolicyExecutionContext());
        }
    }

    [InlineData(true)]
    [InlineData(false)]
    [Theory]
    public async Task SendAsync_EnsureInvoker(bool executionContextSet)
    {
        using var handler = new ResilienceHandler("dummy", _ => Policy.NoOpAsync<HttpResponseMessage>());
        using var invoker = new HttpMessageInvoker(handler);
        using var request = new HttpRequestMessage();

        if (executionContextSet)
        {
            request.SetPolicyExecutionContext(new Context());
        }

        handler.InnerHandler = new TestHandlerStub((r, _) =>
        {
            var invokerProvider = Resilience.Internal.ContextExtensions.CreateMessageInvokerProvider("dummy");
            var requestProvider = Resilience.Internal.ContextExtensions.CreateRequestMessageProvider("dummy");

            Assert.NotNull(invokerProvider(r.GetPolicyExecutionContext()!));
            Assert.Equal(request, requestProvider(r.GetPolicyExecutionContext()!));

            return Task.FromResult(new HttpResponseMessage { StatusCode = HttpStatusCode.Created });
        });

        var response = await invoker.SendAsync(request, default);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }
}
