// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Internal;

internal sealed class ResourceMonitoringOptionsCustomValidator : IValidateOptions<ResourceMonitoringOptions>
{
    public ValidateOptionsResult Validate(string? name, ResourceMonitoringOptions options)
    {
        var builder = new ValidateOptionsResultBuilder();

        if (options.PublishingWindow > options.CollectionWindow)
        {
            builder.AddError(
                $"Value must be <= to {nameof(options.CollectionWindow)} ({options.CollectionWindow}), but is {options.PublishingWindow}.",
                nameof(options.PublishingWindow));
        }

        foreach (var s in options.SourceIpAddresses)
        {
            if (!IPAddress.TryParse(s, out var ipAddress)
                || (ipAddress.AddressFamily != AddressFamily.InterNetwork && ipAddress.AddressFamily != AddressFamily.InterNetworkV6))
            {
                builder.AddError(nameof(options.SourceIpAddresses), "must contain IPv4 or IPv6 addresses only");
            }
        }

        return builder.Build();
    }
}
