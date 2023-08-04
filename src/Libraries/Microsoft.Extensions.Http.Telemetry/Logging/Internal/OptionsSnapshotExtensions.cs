// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Http.Telemetry.Logging.Internal;

internal static class OptionsSnapshotExtensions
{
    public static TOptions GetKeyedOrDefault<TOptions>(this IOptionsSnapshot<TOptions> options, string? serviceKey)
        where TOptions : class
    {
        if (serviceKey is null)
        {
            return options.Value;
        }

        return options.Get(serviceKey);
    }
}
