// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Http.Resilience.Internal;

/// <summary>
/// The cache for named options that allows accessing the last valid options instance.
/// </summary>
/// <typeparam name="TOptions">The type of options.</typeparam>
internal sealed class NamedOptionsCache<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] TOptions>
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
