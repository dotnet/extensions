// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience.FaultInjection.Internal;
using Microsoft.Extensions.Resilience.FaultInjection;
using Polly;
using Xunit;

namespace Microsoft.Extensions.Http.Resilience.FaultInjection.Test;

public class HttpClientChaosPolicyFactoryTest
{
    private readonly string _testOptionsGroupName = "TestGroupName";
    private readonly ChaosPolicyOptionsGroup _testChaosPolicyOptionsGroup;
    private readonly IHttpClientChaosPolicyFactory _testPolicyFactory;

    public HttpClientChaosPolicyFactoryTest()
    {
        _testChaosPolicyOptionsGroup = new ChaosPolicyOptionsGroup
        {
            HttpResponseInjectionPolicyOptions = new HttpResponseInjectionPolicyOptions
            {
                Enabled = true,
                FaultInjectionRate = 0.4
            }
        };

        var services = new ServiceCollection();
        Action<HttpFaultInjectionOptionsBuilder> action =
            builder => builder.Configure(
                options =>
                {
                    options.ChaosPolicyOptionsGroups.Add(_testOptionsGroupName, _testChaosPolicyOptionsGroup);
                });
        services
            .AddLogging()
            .AddHttpClientFaultInjection(action);

        using var provider = services.BuildServiceProvider();
        _testPolicyFactory = provider.GetRequiredService<IHttpClientChaosPolicyFactory>();
    }

    [Fact]
    public void CreateInjectHttpResponsePolicy_WithDelegateFunctions_ShouldReturnInstance()
    {
        var policy = _testPolicyFactory.CreateHttpResponsePolicy();
        Assert.NotNull(policy);
    }

    [Fact]
    public async Task GetEnabledAsync_ShouldReturnEnabled()
    {
        var context = new Context();
        context.WithFaultInjection(_testOptionsGroupName);

        var result = await ((HttpClientChaosPolicyFactory)_testPolicyFactory).GetEnabledAsync(context, CancellationToken.None);
        Assert.Equal(_testChaosPolicyOptionsGroup!.HttpResponseInjectionPolicyOptions!.Enabled, result);
    }

    [Fact]
    public async Task GetEnabledAsync_ContextNoOptionsGroupName_ShouldReturnFalse()
    {
        var services = new ServiceCollection();
        services
            .AddLogging()
            .AddHttpClientFaultInjection();

        using var provider = services.BuildServiceProvider();
        var testPolicyFactory = provider.GetRequiredService<IHttpClientChaosPolicyFactory>();

        var context = new Context();

        var result = await ((HttpClientChaosPolicyFactory)testPolicyFactory).GetEnabledAsync(context, CancellationToken.None);
        Assert.False(result);
    }

    [Fact]
    public async Task GetEnabledAsync_NoOptionsGroupFound_ShouldReturnFalse()
    {
        var context = new Context();
        context.WithFaultInjection("RandomName");

        var result = await ((HttpClientChaosPolicyFactory)_testPolicyFactory).GetEnabledAsync(context, CancellationToken.None);
        Assert.False(result);
    }

    [Fact]
    public async Task GetEnabledAsync_WhenNoHttpResponseInjectionPolicyFoundInOptionsGroup_ShouldReturnFalse()
    {
        var testGroupName = "TestGroup";
        var tesOptionsGroupNoPolicyOptions = new ChaosPolicyOptionsGroup();
        var services = new ServiceCollection();
        Action<HttpFaultInjectionOptionsBuilder> action =
            builder => builder.Configure(
                options =>
                    options.ChaosPolicyOptionsGroups.Add(testGroupName, tesOptionsGroupNoPolicyOptions));

        services
            .AddLogging()
            .AddHttpClientFaultInjection(action);

        using var provider = services.BuildServiceProvider();
        var testPolicyFactory = provider.GetRequiredService<IHttpClientChaosPolicyFactory>();

        var context = new Context();
        context.WithFaultInjection(testGroupName);

        var result = await ((HttpClientChaosPolicyFactory)testPolicyFactory).GetEnabledAsync(context, CancellationToken.None);
        Assert.False(result);
    }

    [Fact]
    public async Task GetInjectionRateAsync_ShouldReturnInjectionRate()
    {
        var context = new Context();
        context.WithFaultInjection(_testOptionsGroupName);

        var result = await ((HttpClientChaosPolicyFactory)_testPolicyFactory).GetInjectionRateAsync(context, CancellationToken.None);
        Assert.Equal(_testChaosPolicyOptionsGroup!.HttpResponseInjectionPolicyOptions!.FaultInjectionRate, result);
    }

    [Fact]
    public async Task GetInjectionRateAsync_ContextNoOptionsGroupName_ShouldReturnZero()
    {
        var services = new ServiceCollection();
        services
            .AddLogging()
            .AddHttpClientFaultInjection();

        using var provider = services.BuildServiceProvider();
        var testPolicyFactory = provider.GetRequiredService<IHttpClientChaosPolicyFactory>();

        var context = new Context();

        var result = await ((HttpClientChaosPolicyFactory)testPolicyFactory).GetInjectionRateAsync(context, CancellationToken.None);
        Assert.Equal(0.0, result);
    }

    [Fact]
    public async Task GetInjectionRateAsync_NoOptionsGroupFound_ShouldReturnZero()
    {
        var context = new Context();
        context.WithFaultInjection("RandomName");

        var result = await ((HttpClientChaosPolicyFactory)_testPolicyFactory).GetInjectionRateAsync(context, CancellationToken.None);
        Assert.Equal(0.0, result);
    }

    [Fact]
    public async Task GetInjectionRateAsync_WhenNoHttpResponseInjectionPolicyFoundInOptionsGroup_ShouldReturnZero()
    {
        var testGroupName = "TestGroup";
        var tesOptionsGroupNoPolicyOptions = new ChaosPolicyOptionsGroup();
        var services = new ServiceCollection();
        Action<HttpFaultInjectionOptionsBuilder> action =
            builder => builder.Configure(
                options =>
                    options.ChaosPolicyOptionsGroups.Add(testGroupName, tesOptionsGroupNoPolicyOptions));

        services
            .AddLogging()
            .AddHttpClientFaultInjection(action);

        using var provider = services.BuildServiceProvider();
        var testPolicyFactory = provider.GetRequiredService<IHttpClientChaosPolicyFactory>();

        var context = new Context();
        context.WithFaultInjection(testGroupName);

        var result = await ((HttpClientChaosPolicyFactory)testPolicyFactory).GetInjectionRateAsync(context, CancellationToken.None);
        Assert.Equal(0.0, result);
    }

    [Fact]
    public async Task GetHttpResponseMessageAsync_ShouldReturnHttpResponseMessage()
    {
        var context = new Context();
        context.WithFaultInjection(_testOptionsGroupName);

        var result = await ((HttpClientChaosPolicyFactory)_testPolicyFactory).GetHttpResponseMessageAsync(context, CancellationToken.None);
        Assert.Equal(_testChaosPolicyOptionsGroup!.HttpResponseInjectionPolicyOptions!.StatusCode, result.StatusCode);
    }
}
