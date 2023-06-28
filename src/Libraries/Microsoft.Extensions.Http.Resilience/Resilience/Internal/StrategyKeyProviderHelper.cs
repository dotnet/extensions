// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net.Http;
using Microsoft.Extensions.Compliance.Classification;
using Microsoft.Extensions.Compliance.Redaction;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Http.Resilience.Internal;

internal static class StrategyKeyProviderHelper
{
    public static void SelectStrategyByAuthority(IServiceCollection services, string strategyName, DataClassification classification)
    {
        UseStrategyKeyProvider(services, strategyName, serviceProvider =>
        {
            var redactor = serviceProvider.GetRequiredService<IRedactorProvider>().GetRedactor(classification);

            return new ByAuthorityStrategyKeyProvider(redactor).GetStrategyKey;
        });
    }

    public static void SelectStrategyBy(IServiceCollection services, string strategyName, Func<IServiceProvider, Func<HttpRequestMessage, string>> selectorFactory)
    {
        UseStrategyKeyProvider(services, strategyName, serviceProvider => selectorFactory(serviceProvider));
    }

    public static Func<HttpRequestMessage, string>? GetStrategyKeyProvider(this IServiceProvider provider, string strategyName)
    {
        return provider.GetRequiredService<IOptionsMonitor<StrategyKeyOptions>>().Get(strategyName).KeyProvider;
    }

    private static void UseStrategyKeyProvider(IServiceCollection services, string strategyName, Func<IServiceProvider, Func<HttpRequestMessage, string>> factory)
    {
        _ = services.AddOptions<StrategyKeyOptions>(strategyName).Configure<IServiceProvider>((options, provider) => options.KeyProvider = factory(provider));
    }
}
