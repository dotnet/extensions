// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace Microsoft.Extensions.Options.Validation.Test;

internal static class HostStartStopExtension
{
    public static async Task StartAndStopAsync(this IHost host)
    {
        try
        {
            await host.StartAsync();
        }
        finally
        {
            await host.StopAsync();
        }
    }
}
