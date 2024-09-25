// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Diagnostics.Probes;

internal sealed class KubernetesProbesOptionsValidator : IValidateOptions<KubernetesProbesOptions>
{
    public ValidateOptionsResult Validate(string? name, KubernetesProbesOptions options)
    {
        var builder = new ValidateOptionsResultBuilder();

        if (options.LivenessProbe.TcpPort == options.StartupProbe.TcpPort
            || options.LivenessProbe.TcpPort == options.ReadinessProbe.TcpPort
            || options.StartupProbe.TcpPort == options.ReadinessProbe.TcpPort)
        {
            builder.AddError("Liveness, startup and readiness probes must use different ports.");
        }

        return builder.Build();
    }
}
