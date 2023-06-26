// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net.Http;

namespace Microsoft.Extensions.Http.Resilience.Internal;

internal sealed class ByCustomSelectorStrategyKeyProvider : IStrategyKeyProvider
{
    private readonly Func<HttpRequestMessage, string> _selector;

    public ByCustomSelectorStrategyKeyProvider(Func<HttpRequestMessage, string> selector)
    {
        _selector = selector;
    }

    public string GetStrategyKey(HttpRequestMessage requestMessage) => _selector(requestMessage);
}
