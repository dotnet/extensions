// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Resilience.Internal;
using Microsoft.Extensions.Telemetry.Metering;
using Polly;
using Xunit;

namespace Microsoft.Extensions.Resilience.Test.Helpers;

[Flags]
public enum MethodArgs
{
    None = 0,

    ConfigureMethod = 1 << 0,

    Configuration = 1 << 1,
}

public abstract class ResilienceTestHelper
{
    protected const string PolicyName = "some-policy-name";
    protected const string DefaultPipelineName = "pipeline-name";
    protected const string DefaultPipelineKey = "pipeline-key";
    protected static readonly IConfigurationSection EmptyConfiguration = new ConfigurationBuilder().Build().GetSection(string.Empty);

    protected IServiceCollection Services { get; } = new ServiceCollection().AddLogging().RegisterMetering();

    protected static IConfigurationSection CreateEmptyConfiguration()
    {
        return new ConfigurationBuilder().Build().GetSection(string.Empty);
    }

    protected static IConfigurationSection CreateConfiguration(string key, string value)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "dummySection:" + key, value }
            })
            .Build()
            .GetSection("dummySection");
    }

    protected static bool HasEmptyArgs(MethodArgs args) => !args.HasFlag(MethodArgs.Configuration) && !args.HasFlag(MethodArgs.ConfigureMethod);

    protected static void AssertOptions<T, TValue>(T options, Func<T, TValue> accessor, TValue? expectedValue, bool useOptions)
        where T : class, new()
    {
        if (!useOptions)
        {
            Assert.Equal(accessor(new T()), accessor(options));
        }
        else
        {
            Assert.Equal(expectedValue, accessor(options));
        }
    }

    public static IEnumerable<object[]> AllCombinations() => GetAllCombinations().Select(v => new object[] { v });

    public static IEnumerable<object[]> ConfigureMethodCombinations() => GetAllCombinations().Where(v => v.HasFlag(MethodArgs.ConfigureMethod)).Select(v => new object[] { v });

    public static IEnumerable<object[]> ConfigurationCombinations() => GetAllCombinations().Where(v => v.HasFlag(MethodArgs.Configuration)).Select(v => new object[] { v });

    public static IEnumerable<MethodArgs> GetAllCombinations()
    {
        yield return MethodArgs.None;
        yield return MethodArgs.ConfigureMethod;
        yield return MethodArgs.Configuration;
        yield return MethodArgs.Configuration | MethodArgs.ConfigureMethod;
    }

    protected IAsyncPolicy<string> CreatePipeline(string name = DefaultPipelineName)
        => Services.BuildServiceProvider().GetRequiredService<IResiliencePipelineFactory>().CreatePipeline<string>(name, DefaultPipelineKey);

    protected IAsyncPolicy<T> CreatePipeline<T>(string name = DefaultPipelineName)
        => Services.BuildServiceProvider().GetRequiredService<IResiliencePipelineFactory>().CreatePipeline<T>(name, DefaultPipelineKey);

    internal static string GetOptionsName(SupportedPolicies policy, string pipelineName, string policyName) => OptionsNameHelper.GetPolicyOptionsName(policy, pipelineName, policyName);
}
