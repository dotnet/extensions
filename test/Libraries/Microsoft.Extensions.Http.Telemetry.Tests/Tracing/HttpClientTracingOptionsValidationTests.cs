// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting.Testing;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.Extensions.Http.Telemetry.Tracing.Test;

public class HttpClientTracingOptionsValidationTests
{
    [Theory]
    [MemberData(nameof(ConfigureHttpClientTracingDelegates))]
    public async Task HttpClientTracingOptions_RouteParameterDataClasses_ShouldNotBeNull(
        Action<IServiceCollection> configureHttpClientTracing)
    {
        using var host = FakeHost
            .CreateBuilder()
            .ConfigureServices((_, services) => configureHttpClientTracing(services))
            .Build();

        var exception = await Assert.ThrowsAsync<OptionsValidationException>(() => host.StartAsync());
        Assert.Contains(nameof(HttpClientTracingOptions.RouteParameterDataClasses), exception.Message);
    }

    public static IEnumerable<object[]> ConfigureHttpClientTracingDelegates
    {
        get
        {
            yield return new object[]
            {
                (IServiceCollection services) =>
                {
                    services.AddOpenTelemetry().WithTracing(builder => builder.AddHttpClientTracing());
                    services.Configure<HttpClientTracingOptions>(
                        options => options.RouteParameterDataClasses = null!);
                }
            };

            yield return new object[]
            {
                (IServiceCollection services) =>
                {
                    services.AddOpenTelemetry().WithTracing(builder => builder.AddHttpClientTracing(
                        options => options.RouteParameterDataClasses = null!));
                }
            };
        }
    }
}
