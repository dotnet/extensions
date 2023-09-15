// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.Extensions.Diagnostics.Latency.Test;

public class LatencyRegistryExtensionsTest
{
    [Fact]
    public void LatencyRegistryExtension_NullArguments()
    {
        Assert.Throws<ArgumentNullException>(
            () => LatencyRegistryExtensions.RegisterCheckpointNames(new ServiceCollection(), null!));
        Assert.Throws<ArgumentNullException>(
            () => LatencyRegistryExtensions.RegisterCheckpointNames(null!, new string[0]));
        Assert.Throws<ArgumentNullException>(
            () => LatencyRegistryExtensions.RegisterMeasureNames(null!, new string[0]));
        Assert.Throws<ArgumentNullException>(
            () => LatencyRegistryExtensions.RegisterMeasureNames(new ServiceCollection(), null!));
        Assert.Throws<ArgumentNullException>(
            () => LatencyRegistryExtensions.RegisterTagNames(null!, new string[0]));
        Assert.Throws<ArgumentNullException>(
            () => LatencyRegistryExtensions.RegisterTagNames(new ServiceCollection(), null!));
    }

    [Fact]
    public void LatencyRegistryExtension_EmptyNames()
    {
        Assert.Throws<ArgumentException>(() => LatencyRegistryExtensions.RegisterCheckpointNames(new ServiceCollection(), ""));
        Assert.Throws<ArgumentException>(() => LatencyRegistryExtensions.RegisterMeasureNames(new ServiceCollection(), ""));
        Assert.Throws<ArgumentException>(() => LatencyRegistryExtensions.RegisterTagNames(new ServiceCollection(), ""));
    }

    [Fact]
    public void LatencyRegistryExtension_BasicFunctionality()
    {
        var services = RegisterNames(new ServiceCollection());
        var serviceProvider = services.BuildServiceProvider();
        Assert.NotNull(serviceProvider);

        var option = serviceProvider.GetService<IOptions<LatencyContextRegistrationOptions>>();
        Assert.NotNull(option);
        Assert.NotNull(option!.Value);
        CheckNumberOfRegisteredNames(option.Value!);
    }

    [Fact]
    public void LatencyRegistry_CreateOption()
    {
        var lcro = new LatencyContextRegistrationOptions();
        var chk = new[] { "ca", "cb", "cc" };
        var tags = new[] { "ta", "tb", "tc" };
        var measures = new[] { "ma", "mb", "mc" };
        lcro.CheckpointNames = chk;
        lcro.MeasureNames = measures;
        lcro.TagNames = tags;

        Assert.Equal(chk, lcro.CheckpointNames);
        Assert.Equal(measures, lcro.MeasureNames);
        Assert.Equal(tags, lcro.TagNames);
    }

    private static IServiceCollection RegisterNames(IServiceCollection services)
    {
        services.RegisterCheckpointNames(new[] { "ca" });
        services.RegisterMeasureNames(new[] { "ma" });
        services.RegisterMeasureNames(new[] { "mb" });
        services.RegisterTagNames(new[] { "ta" });
        services.RegisterTagNames(new[] { "tb", "tc" });

        return services;
    }

    private static void CheckNumberOfRegisteredNames(LatencyContextRegistrationOptions lcro)
    {
        Assert.True(lcro.CheckpointNames.Count == 1);
        Assert.True(lcro.MeasureNames.Count == 2);
        Assert.True(lcro.TagNames.Count == 3);
    }
}
