// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Options;
using Validation = Microsoft.Extensions.Options.Validation;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Internal;

internal sealed class WindowsCountersOptionsCustomValidator : IValidateOptions<WindowsCountersOptions>
{
    public ValidateOptionsResult Validate(string? name, WindowsCountersOptions options)
    {
        var builder = new ValidateOptionsResultBuilder();
        foreach (var s in options.InstanceIpAddresses)
        {
            if (!IPAddress.TryParse(s, out var ipAddress)
                || ipAddress.AddressFamily != AddressFamily.InterNetwork)
            {
                builder.AddError(nameof(options.InstanceIpAddresses), "must only contain IPv4 addresses");
            }
        }

        return builder.Build();
    }
}
