// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net.Http;
using Microsoft.Extensions.Compliance.Classification;
using Microsoft.Extensions.Compliance.Redaction;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Http.Resilience.Internal;

internal static class StrategyKeyProviderHelper
{
    public static void SelectStrategyByAuthority(IServiceCollection services, string strategyName, DataClassification classification)
    {
        UseStrategyKeyProvider(services, strategyName, serviceProvider =>
        {
            var redactor = serviceProvider.GetRequiredService<IRedactorProvider>().GetRedactor(classification);

            return new ByAuthorityStrategyKeyProvider(redactor);
        });
    }

    public static void SelectStrategyBy(IServiceCollection services, string strategyName, Func<IServiceProvider, Func<HttpRequestMessage, string>> selectorFactory)
    {
        UseStrategyKeyProvider(services, strategyName, serviceProvider =>
        {
            return new ByCustomSelectorStrategyKeyProvider(selectorFactory(serviceProvider));
        });
    }

    public static IStrategyKeyProvider? GetStrategyKeyProvider(this IServiceProvider provider, string strategyName)
    {
        return provider.GetService<INamedServiceProvider<IStrategyKeyProvider>>()?.GetService(strategyName);
    }

    private static void UseStrategyKeyProvider(IServiceCollection services, string strategyName, Func<IServiceProvider, IStrategyKeyProvider> factory)
    {
        _ = services.AddNamedSingleton(strategyName, factory);
    }
}
