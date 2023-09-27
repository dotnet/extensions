// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Http.Resilience.Internal;

internal static class PipelineKeyProviderHelper
{
    public static void SelectPipelineByAuthority(IServiceCollection services, string pipelineName)
    {
        UsePipelineKeyProvider(services, pipelineName, serviceProvider =>
        {
            return new ByAuthorityPipelineKeyProvider().GetPipelineKey;
        });
    }

    public static void SelectPipelineBy(IServiceCollection services, string pipelineName, Func<IServiceProvider, Func<HttpRequestMessage, string>> selectorFactory)
    {
        UsePipelineKeyProvider(services, pipelineName, serviceProvider => selectorFactory(serviceProvider));
    }

    public static Func<HttpRequestMessage, string>? GetPipelineKeyProvider(this IServiceProvider provider, string pipelineName)
    {
        return provider.GetRequiredService<IOptionsMonitor<PipelineKeyOptions>>().Get(pipelineName).KeyProvider;
    }

    private static void UsePipelineKeyProvider(IServiceCollection services, string pipelineName, Func<IServiceProvider, Func<HttpRequestMessage, string>> factory)
    {
        _ = services.AddOptions<PipelineKeyOptions>(pipelineName).Configure<IServiceProvider>((options, provider) => options.KeyProvider = factory(provider));
    }
}
