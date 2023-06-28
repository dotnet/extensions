// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Http.Resilience.Internal;

internal sealed class NamedOptionsCache<TOptions>
{
    public NamedOptionsCache(string clientId, IOptionsMonitor<TOptions> optionsMonitor)
    {
        Options = optionsMonitor.Get(clientId);

        _ = optionsMonitor.OnChange((options, name) =>
        {
            if (name == clientId)
            {
                Options = options;
            }
        });
    }

    public TOptions Options { get; private set; }
}
