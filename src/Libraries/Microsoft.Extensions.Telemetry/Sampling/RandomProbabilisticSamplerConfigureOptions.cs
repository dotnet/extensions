// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Diagnostics.Sampling;

internal sealed class RandomProbabilisticSamplerConfigureOptions : IConfigureOptions<RandomProbabilisticSamplerOptions>
{
    private const string RandomProbabilisticSamplerKey = "RandomProbabilisticSampler";
    private readonly IConfiguration _configuration;

    public RandomProbabilisticSamplerConfigureOptions(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public void Configure(RandomProbabilisticSamplerOptions options)
    {
        if (_configuration is null)
        {
            return;
        }

        IConfigurationSection section = _configuration.GetSection(RandomProbabilisticSamplerKey);
        if (!section.Exists())
        {
            return;
        }

        RandomProbabilisticSamplerOptions? parsedOptions = section.Get<RandomProbabilisticSamplerOptions>();
        if (parsedOptions is null)
        {
            return;
        }

        foreach (var rule in parsedOptions.Rules)
        {
            options.Rules.Add(rule);
        }
    }
}
