// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net.Http;
using Microsoft.Extensions.Compliance.Classification;
using Microsoft.Extensions.Compliance.Redaction;
using Microsoft.Extensions.Http.Resilience.Internal;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Http.Resilience;

/// <summary>
/// Extensions for <see cref="IHttpResilienceStrategyBuilder"/>.
/// </summary>
public static class HttpResilienceStrategyBuilderExtensions
{
    /// <summary>
    /// Instructs the underlying builder to select the strategy instance by redacted authority (scheme + host + port).
    /// </summary>
    /// <param name="builder">The builder instance.</param>
    /// <param name="classification">The data class associated with the authority.</param>
    /// <returns>The same builder instance.</returns>
    /// <remarks>The authority is redacted using <see cref="Redactor"/> retrieved for <paramref name="classification"/>.</remarks>
    public static IHttpResilienceStrategyBuilder SelectStrategyByAuthority(this IHttpResilienceStrategyBuilder builder, DataClassification classification)
    {
        _ = Throw.IfNull(builder);

        StrategyKeyProviderHelper.SelectStrategyByAuthority(builder.Services, builder.StrategyName, classification);

        return builder;
    }

    /// <summary>
    /// Instructs the underlying builder to select the strategy instance by custom selector.
    /// </summary>
    /// <param name="builder">The builder instance.</param>
    /// <param name="selectorFactory">The factory that returns a key selector.</param>
    /// <returns>The same builder instance.</returns>
    /// <remarks>The strategy key is used in metrics and logs, so don't return any sensitive values.</remarks>
    public static IHttpResilienceStrategyBuilder SelectStrategyBy(this IHttpResilienceStrategyBuilder builder, Func<IServiceProvider, Func<HttpRequestMessage, string>> selectorFactory)
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNull(selectorFactory);

        StrategyKeyProviderHelper.SelectStrategyBy(builder.Services, builder.StrategyName, selectorFactory);

        return builder;
    }
}
