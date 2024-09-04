// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Microsoft.Extensions.Diagnostics.Probes.Test;

public class KubernetesProbesOptionsValidatorTests
{
    [Fact]
    public void Validator_DefaultValues_Succeeds()
    {
        var options = new KubernetesProbesOptions();
        var result = new KubernetesProbesOptionsValidator().Validate(nameof(options), options);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Validator_GivenValidOptions_Succeeds()
    {
        var options = new KubernetesProbesOptions();
        options.LivenessProbe.TcpPort = 2305;
        options.StartupProbe.TcpPort = 2306;
        options.ReadinessProbe.TcpPort = 2307;

        var result = new KubernetesProbesOptionsValidator().Validate(nameof(options), options);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Validator_GivenInvalidOptions_Fails()
    {
        var options = new KubernetesProbesOptions();
        options.LivenessProbe.TcpPort = 2305;
        options.StartupProbe.TcpPort = 2305;
        options.ReadinessProbe.TcpPort = 2307;

        var validator = new KubernetesProbesOptionsValidator();
        var result = validator.Validate(nameof(options), options);
        Assert.True(result.Failed);

        options.LivenessProbe.TcpPort = 2305;
        options.StartupProbe.TcpPort = 2306;
        options.ReadinessProbe.TcpPort = 2305;
        result = validator.Validate(nameof(options), options);
        Assert.True(result.Failed);

        options.LivenessProbe.TcpPort = 2305;
        options.StartupProbe.TcpPort = 2306;
        options.ReadinessProbe.TcpPort = 2306;
        result = validator.Validate(nameof(options), options);
        Assert.True(result.Failed);

        options.LivenessProbe.TcpPort = 2305;
        options.StartupProbe.TcpPort = 2305;
        options.ReadinessProbe.TcpPort = 2305;
        result = validator.Validate(nameof(options), options);
        Assert.True(result.Failed);
    }
}
