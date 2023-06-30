// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Http.Resilience.Internal;

/// <summary>
/// The cache for named options that allows accessing the last valid options instance.
/// </summary>
/// <typeparam name="TOptions">The type of options.</typeparam>
internal sealed class NamedOptionsCache<TOptions>
{
    public NamedOptionsCache(string optionsName, IOptionsMonitor<TOptions> optionsMonitor)
    {
        Options = optionsMonitor.Get(optionsName);

        _ = optionsMonitor.OnChange((options, name) =>
        {
            if (name == optionsName)
            {
                Options = options;
            }
        });
    }

    public TOptions Options { get; private set; }
}
