// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience.Internal.Validators;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Resilience;
using Microsoft.Extensions.Resilience.Internal;

namespace Microsoft.Extensions.Http.Resilience.Internal;

internal static class FallbackHelper
{
    public const string HandlerPostfix = "fallback-handler";

    public static void AddFallbackPolicy(IHttpResiliencePipelineBuilder builder, string optionsName, Action<OptionsBuilder<FallbackClientHandlerOptions>> configure)
    {
        var pipelineName = builder.PipelineName;

        _ = builder.Services.AddRequestCloner();
        _ = builder.AddPolicy<HttpResponseMessage, FallbackClientHandlerOptions, FallbackClientHandlerOptionsValidator>(
            optionsName,
            options => configure(options),
            (builder, options, serviceProvider) => builder.AddFallbackPolicy(
                "DefaultFallbackPolicy",
                CreateProvider(serviceProvider, pipelineName, optionsName),
                options.FallbackPolicyOptions));
    }

    private static FallbackScenarioTaskProvider<HttpResponseMessage> CreateProvider(IServiceProvider serviceProvider, string pipelineName, string optionsName)
    {
        var cloner = serviceProvider.GetRequiredService<IRequestClonerInternal>();
        var monitor = serviceProvider.GetRequiredService<IOptionsMonitor<FallbackClientHandlerOptions>>();
        var invokerProvider = ContextExtensions.CreateMessageInvokerProvider(pipelineName);
        var requestProvider = ContextExtensions.CreateRequestMessageProvider(pipelineName);

        return args =>
        {
            var request = requestProvider(args.Context)!;
            var invoker = invokerProvider(args.Context)!;
            var fallbackUrl = monitor.Get(optionsName).BaseFallbackUri!;

            // Request is cloned as the private property "_sendStatus" of the initial HttpRequestMessage
            // is set to 1 (i.e. sent) during the execution of the SendAsync method.
            using var snapshot = cloner.CreateSnapshot(request);
            var newRequest = snapshot.Create().ReplaceHost(fallbackUrl);

            return invoker.SendAsync(newRequest, args.CancellationToken);
        };
    }
}
