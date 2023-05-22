// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;

namespace Microsoft.Extensions.Http.Resilience.Internal;

internal sealed class ByCustomSelectorPipelineKeyProvider : IPipelineKeyProvider
{
    private readonly PipelineKeySelector _selector;

    public ByCustomSelectorPipelineKeyProvider(PipelineKeySelector selector)
    {
        _selector = selector;
    }

    public string GetPipelineKey(HttpRequestMessage requestMessage)
    {
        return _selector(requestMessage);
    }
}
