// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Testing;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.AspNetCore.Telemetry.Test;

public class HttpTracingOptionsValidationTests
{
    [Theory]
    [MemberData(nameof(ConfigureHttpTracingDelegates))]
    public async Task HttpTracingOptions_RequiredProperties_ShouldNotBeNull(
        Action<IServiceCollection> configureHttpTracing)
    {
        using var host = FakeHost
            .CreateBuilder()
            .ConfigureServices(configureHttpTracing)
            .Build();

        try
        {
            var exception = await Assert.ThrowsAsync<OptionsValidationException>(() => host.StartAsync());
            Assert.Contains(nameof(HttpTracingOptions.RouteParameterDataClasses), exception.Message);
            Assert.Contains(nameof(HttpTracingOptions.ExcludePathStartsWith), exception.Message);
        }
        finally
        {
            await host.StopAsync();
        }
    }

    public static IEnumerable<object[]> ConfigureHttpTracingDelegates
    {
        get
        {
            yield return new object[]
            {
                (IServiceCollection services) =>
                {
                    services.AddOpenTelemetry().WithTracing(builder => builder.AddHttpTracing());
                    services.Configure<HttpTracingOptions>(options =>
                    {
                        options.RouteParameterDataClasses = null!;
                        options.ExcludePathStartsWith = null!;
                    });
                }
            };

            yield return new object[]
            {
                (IServiceCollection services) =>
                {
                    services.AddOpenTelemetry().WithTracing(builder =>
                        builder.AddHttpTracing(options =>
                        {
                            options.RouteParameterDataClasses = null!;
                            options.ExcludePathStartsWith = null!;
                        }));
                }
            };
        }
    }
}
