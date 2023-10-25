// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Diagnostics.Latency.Internal;

namespace Microsoft.Extensions.Diagnostics.Latency.Test.Internal;

internal static class LatencyContextExtensions
{
    public static bool IsRegistered(this Registry registry, string name)
    {
        return registry.GetRegisteredKeyIndex(name) > -1;
    }
}
