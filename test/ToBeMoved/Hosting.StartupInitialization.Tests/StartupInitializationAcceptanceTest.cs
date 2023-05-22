// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting.Testing.Internal;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Microsoft.Shared.Diagnostics;
using Xunit;

namespace Microsoft.Extensions.Hosting.Testing.StartupInitialization.Test;

public class StartupInitializationAcceptanceTest
{
    [Fact]
    public async Task Initialization_Functions_Are_Executed_On_Startup_In_Async_Manner_And_Logs_Message()
    {
        using var host = FakeHost.CreateBuilder()
              .ConfigureServices((_, services) => services
              .AddSingleton<Database>()
              .AddStartupInitialization()
              .AddInitializer(async static (sp, _) =>
                  {
                      var db = sp.GetService<Database>();

                      Assert.NotNull(db);
                      await db!.Initialize();
                  }))
              .Build();

        await host.StartAsync();
        await host.StopAsync();

        var logMessages = host.GetFakeLogCollector().GetSnapshot().Select(x => x.Message);

        Assert.Contains(Database.LogMessage, logMessages);
    }

    [Fact]
    public async Task Initialization_Functions_Are_Executed_On_Startup_In_Async_Manner_And_Logs_Message_When_Using_Interface_Registration()
    {
        using var host = FakeHost.CreateBuilder()
              .ConfigureServices((_, services) => services
              .AddStartupInitialization()
              .AddInitializer<DatabaseInitializer>())
              .Build();

        await host.StartAsync();
        await host.StopAsync();

        var logMessages = host.GetFakeLogCollector().GetSnapshot().Select(x => x.Message);

        Assert.Contains(Database.LogMessage, logMessages);
    }

    [Fact]
    public void Initializers_Are_Indempotent_When_Provided_As_Interface()
    {
        using var sp = new ServiceCollection()
                .AddLogging()
                .AddStartupInitialization()
                    .AddInitializer<DatabaseInitializer>()
                    .AddInitializer<DatabaseInitializer>()
                    .AddInitializer<DatabaseInitializer>()
                    .AddInitializer<DatabaseInitializer>()
                .Services
                .BuildServiceProvider();

        var i = sp.GetRequiredService<IEnumerable<IStartupInitializer>>();

        Assert.Single(i);
    }

    [Fact]
    public void Initializers_Are_Not_Indempotent_When_Provided_As_Anonymous_Function()
    {
        using var sp = new ServiceCollection()
                .AddLogging()
                .AddStartupInitialization()
                    .AddInitializer((_, _) => Task.CompletedTask)
                    .AddInitializer((_, _) => Task.CompletedTask)
                    .AddInitializer((_, _) => Task.CompletedTask)
                    .AddInitializer((_, _) => Task.CompletedTask)
                    .AddInitializer((_, _) => Task.CompletedTask)
                .Services
                .BuildServiceProvider();

        var i = sp.GetRequiredService<IEnumerable<IStartupInitializer>>().ToArray();

        Assert.Equal(5, i.Length);
    }

    [Fact]
    public async Task Initialization_Functions_Are_Not_Executed_On_Startup_So_There_Is_No_Log_Message()
    {
        using var host = FakeHost.CreateBuilder()
              .ConfigureServices((_, services) => services
              .AddSingleton<Database>())
              .Build();

        await host.StartAsync();
        await host.StopAsync();

        var logMessages = host.GetFakeLogCollector().GetSnapshot().Select(x => x.Message);

        Assert.DoesNotContain(Database.LogMessage, logMessages);
    }

    [Fact]
    public void When_Registering_Multiple_Hosted_Services_StartupService_Is_First()
    {
        const int RegisteredHostedServices = 4;

        using var host = FakeHost.CreateBuilder()
             .ConfigureServices((_, services) => services
                 .AddSingleton<Database>()
                 .AddHostedService<DummyHostedService>()
                 .AddHostedService<DummyHostedService2>()
                 .AddHostedService<DummyHostedService3>()
                 .AddStartupInitialization()
                 .AddInitializer(async static (sp, _) =>
                     {
                         var db = sp.GetService<Database>();

                         Assert.NotNull(db);
                         await db!.Initialize();
                     }))
                 .Build();

        var jobs = host.Services
            .GetRequiredService<IEnumerable<IHostedService>>()
            ?.ToArray();

        Assert.NotNull(jobs);

        // In case FakeHost is adding some stuff.
        Assert.True(jobs!.Length >= RegisteredHostedServices);
        Assert.IsAssignableFrom<StartupHostedService>(jobs[0]);
    }

    [Fact]
    public async Task Initialization_Function_Times_Out_When_It_Takes_Longer_Than_Options()
    {
        var fiveSeconds = TimeSpan.FromSeconds(5);
        var timeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);

        using var host = FakeHost.CreateBuilder()
            .ConfigureServices(s => s
                .AddSingleton<System.TimeProvider>(timeProvider)
                .AddStartupInitialization(x => x.Timeout = fiveSeconds)
                .AddInitializer((_, ct) =>
                    {
                        timeProvider.Advance(fiveSeconds);
                        return Task.Delay(-1, ct);
                    }))
            .Build();

        var e = await Assert.ThrowsAsync<TaskCanceledException>(() => host.StartAsync());

        Assert.Contains(fiveSeconds.ToString(), e.Message);
        Assert.Contains(nameof(StartupInitializationOptions), e.Message);
    }

    [Fact]
    public async Task Initialization_Function_Is_Cancelled_Without_Message_When_HostBuilder_Is_Canceled()
    {
        var fiveSeconds = TimeSpan.FromSeconds(5);
        var timeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);

        var twoSeconds = TimeSpan.FromSeconds(2);
        using var cts = timeProvider.CreateCancellationTokenSource(twoSeconds);
        using var host = FakeHost.CreateBuilder()
            .ConfigureServices(s => s
                .AddSingleton<TimeProvider>(timeProvider)
                .AddStartupInitialization(x => x.Timeout = fiveSeconds)
                .AddInitializer((_, ct) =>
                {
                    timeProvider.Advance(twoSeconds);
                    return Task.Delay(-1, ct);
                }))
            .Build();

        var e = await Assert.ThrowsAsync<TaskCanceledException>(() => host.StartAsync(cts.Token));

        Assert.DoesNotContain(fiveSeconds.ToString(), e.Message);
        Assert.DoesNotContain(nameof(StartupInitializationOptions), e.Message);
    }

    [Fact]
    public void Can_Use_Configuration_Section_To_Configure_StartupInitializationOptions()
    {
        var timeout = TimeSpan.FromSeconds(29);

        var o = new ServiceCollection()
            .AddStartupInitialization(TestResources.GetSection(timeout))
            .Services
            .BuildServiceProvider()
            .GetRequiredService<IOptions<StartupInitializationOptions>>();

        Assert.NotNull(o?.Value);
        Assert.Equal(timeout, o!.Value.Timeout);
    }

    [Theory]
    [InlineData(60000)]
    [InlineData(4)]
    public void When_Setting_Initialization_Timeout_Out_Of_Boundary_Validator_Throws(int seconds)
    {
        var o = new ServiceCollection()
            .AddStartupInitialization(x => x.Timeout = TimeSpan.FromSeconds(seconds))
            .Services
            .BuildServiceProvider()
            .GetRequiredService<IOptions<StartupInitializationOptions>>();

        Assert.Throws<OptionsValidationException>(() => o?.Value);
    }

    [Fact]
    public void StartupHostedService_Gets_Registered_Only_Once_In_DI_And_It_Is_First()
    {
        using var sp = new ServiceCollection()
            .AddStartupInitialization()
            .Services
            .AddStartupInitialization()
            .Services
            .AddStartupInitialization()
            .Services
            .AddStartupInitialization()
            .Services
            .AddStartupInitialization(_ => { })
            .Services
            .AddStartupInitialization(_ => { })
            .Services
            .BuildServiceProvider();

        var s = sp.GetRequiredService<IEnumerable<IHostedService>>().ToArray();

        Assert.IsAssignableFrom<StartupHostedService>(s[0]);
        Assert.Equal(1, s.Count(x => x is StartupHostedService));
    }

    [Fact]
    public void When_Debugger_Is_Attached_Hosted_Service_Timeout_Is_Set_To_Infinite()
    {
        using var provider = new ServiceCollection()
            .AddAttachedDebuggerState()
            .AddStartupInitialization()
            .AddInitializer((_, _) => Task.CompletedTask)
            .Services
            .BuildServiceProvider();

        var service = provider
            .GetRequiredService<IEnumerable<IHostedService>>()
            .FirstOrDefault(x => x is StartupHostedService);

        Assert.IsAssignableFrom<StartupHostedService>(service);
        Assert.Equal(((StartupHostedService)service!).Timeout, System.Threading.Timeout.InfiniteTimeSpan);
    }
}
