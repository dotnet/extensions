// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.Extensions.Diagnostics.Probes.Test;

public class KubernetesProbesOptionsValidatorTests
{
    [Fact]
    public void Validator_DefaultValues_Succeeds()
    {
        var options = new KubernetesProbesOptions();
        ValidateOptionsResult result = new KubernetesProbesOptionsValidator().Validate(nameof(options), options);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Validator_GivenValidOptions_Succeeds()
    {
        var options = new KubernetesProbesOptions();
        options.LivenessProbe.TcpPort = 2305;
        options.StartupProbe.TcpPort = 2306;
        options.ReadinessProbe.TcpPort = 2307;

        ValidateOptionsResult result = new KubernetesProbesOptionsValidator().Validate(nameof(options), options);

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
        ValidateOptionsResult result = validator.Validate(nameof(options), options);
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

    [Fact]
    public async Task Validator_WhenHostStarts_Succeeds()
    {
        using IHost host = CreateHost(services =>
        {
            services.AddKubernetesProbes(options =>
            {
                options.LivenessProbe.TcpPort = 22305;
                options.StartupProbe.TcpPort = 22306;
                options.ReadinessProbe.TcpPort = 22307;
            }).AddHealthChecks();
        });

        try
        {
            host.Start();
            await host.StopAsync();
        }
        catch (OptionsValidationException ex)
        {
            Assert.Fail("Unexpected OptionsValidationException: " + ex.Message);
        }
        catch (Exception ex)
        {
            Assert.Fail("Unexpected exception: " + ex.Message);
            throw;
        }
    }

    [Fact]
    public void Validator_WhenHostStarts_Fails()
    {
        Action action = () =>
        {
            using IHost host = CreateHost(services =>
            {
                services.AddKubernetesProbes(options =>
                {
                    options.LivenessProbe.TcpPort = 22305;
                    options.StartupProbe.TcpPort = 22305;
                    options.ReadinessProbe.TcpPort = 22307;
                }).AddHealthChecks();
            });

            host.Start();
        };

        Assert.Throws<OptionsValidationException>(action);
    }

    private static IHost CreateHost(Action<IServiceCollection> configureServices)
    {
        return Host.CreateDefaultBuilder()
            .ConfigureServices(configureServices)
            .Build();
    }
}
