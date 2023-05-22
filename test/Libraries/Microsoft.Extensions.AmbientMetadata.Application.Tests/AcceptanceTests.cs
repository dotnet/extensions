// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading.Tasks;
using AutoFixture;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Testing;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.Extensions.AmbientMetadata.Test;

public class AcceptanceTests
{
    private static readonly Fixture _fixture = new();
    private static readonly ApplicationMetadata _metadata = new()
    {
        BuildVersion = _fixture.Create<string>(),
        DeploymentRing = _fixture.Create<string>(),
        ApplicationName = _fixture.Create<string>(),
    };

    [Theory]
    [InlineData("ambientmetadata:application")]
    [InlineData(null)]
    public async Task UseApplicationMetadata_CreatesPopulatesAndRegistersOptions(string? sectionName) =>
        await RunAsync(
            (options, hostEnvironment) =>
            {
                options.BuildVersion.Should().Be(_metadata.BuildVersion);
                options.DeploymentRing.Should().Be(_metadata.DeploymentRing);
                options.ApplicationName.Should().Be(_metadata.ApplicationName);
                options.EnvironmentName.Should().Be(hostEnvironment.EnvironmentName);

                return Task.CompletedTask;
            },
            sectionName);

    private static async Task RunAsync(Func<ApplicationMetadata, IHostEnvironment, Task> func, string? sectionName)
    {
        using var host = await FakeHost.CreateBuilder()

            // need to set applicationName manually, because
            // netfx console test runner cannot get assebly name
            // to be able to set it automatically
            // see https://source.dot.net/#Microsoft.Extensions.Hosting/HostBuilder.cs,240
            .ConfigureHostConfiguration("applicationname", _metadata.ApplicationName)
            .UseApplicationMetadata(sectionName ?? "ambientmetadata:application")
            .ConfigureServices((_, services) => services.AddApplicationMetadata(metadata =>
            {
                metadata.BuildVersion = _metadata.BuildVersion;
                metadata.DeploymentRing = _metadata.DeploymentRing;
            }))
            .StartAsync();

        await func(host.Services.GetRequiredService<IOptions<ApplicationMetadata>>().Value,
                   host.Services.GetRequiredService<IHostEnvironment>());
        await host.StopAsync();
    }
}
