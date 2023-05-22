// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Compliance.Classification;
using Microsoft.Extensions.Compliance.Redaction;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Http.Resilience.Internal;

internal static class PipelineKeyProviderHelper
{
    public static void SelectPipelineByAuthority(IServiceCollection services, string pipelineName, DataClassification classification)
    {
        UsePipelineKeyProvider(services, pipelineName, serviceProvider =>
        {
            var redactor = serviceProvider.GetRequiredService<IRedactorProvider>().GetRedactor(classification);

            return ActivatorUtilities.CreateInstance<ByAuthorityPipelineKeyProvider>(serviceProvider, redactor, classification);
        });
    }

    public static void SelectPipelineBy(IServiceCollection services, string pipelineName, Func<IServiceProvider, PipelineKeySelector> selectorFactory)
    {
        UsePipelineKeyProvider(services, pipelineName, serviceProvider =>
        {
            var selector = selectorFactory(serviceProvider);

            return ActivatorUtilities.CreateInstance<ByCustomSelectorPipelineKeyProvider>(serviceProvider, selector);
        });
    }

    public static IPipelineKeyProvider? GetPipelineKeyProvider(this IServiceProvider provider, string pipelineName)
    {
        return provider.GetService<INamedServiceProvider<IPipelineKeyProvider>>()?.GetService(pipelineName);
    }

    private static void UsePipelineKeyProvider(IServiceCollection services, string pipelineName, Func<IServiceProvider, IPipelineKeyProvider> factory)
    {
        _ = services.AddNamedSingleton(pipelineName, factory);
    }
}
