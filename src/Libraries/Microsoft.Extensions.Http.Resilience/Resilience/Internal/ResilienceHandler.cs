// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Polly;

namespace Microsoft.Extensions.Http.Resilience.Internal;

/// <summary>
/// Base class for resilience handler, i.e. handlers that use resilience pipelines to send the requests.
/// </summary>
internal sealed class ResilienceHandler : PolicyHttpMessageHandler
{
    private readonly Lazy<HttpMessageInvoker> _invoker;
    private readonly Action<Context, Lazy<HttpMessageInvoker>> _invokerSetter;
    private readonly Action<Context, HttpRequestMessage> _requestSetter;

    public ResilienceHandler(string pipelineName, Func<HttpRequestMessage, IAsyncPolicy<HttpResponseMessage>> policySelector)
        : base(policySelector)
    {
        // Stryker disable once boolean : no means to test this
        _invoker = new Lazy<HttpMessageInvoker>(() => new HttpMessageInvoker(InnerHandler!), true);
        _invokerSetter = ContextExtensions.CreateMessageInvokerSetter(pipelineName);
        _requestSetter = ContextExtensions.CreateRequestMessageSetter(pipelineName);
    }

    /// <inheritdoc/>
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var created = false;
        if (request.GetPolicyExecutionContext() is not Context context)
        {
            context = new Context();
            request.SetPolicyExecutionContext(context);
            created = true;
        }

        // set common properties to the context
        context.SetRequestMetadata(request);
        _invokerSetter(context, _invoker);
        _requestSetter(context, request);

        try
        {
            return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            if (created)
            {
                request.SetPolicyExecutionContext(null);
            }
        }
    }
}
