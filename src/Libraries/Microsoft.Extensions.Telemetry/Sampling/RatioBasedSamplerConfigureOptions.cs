// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Diagnostics.Sampling;

internal sealed class RatioBasedSamplerConfigureOptions : IConfigureOptions<RatioBasedSamplerOptions>
{
    private const string RatioBasedSamplerKey = "RatioBasedSampler";
    private readonly IConfiguration _configuration;

    public RatioBasedSamplerConfigureOptions(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public void Configure(RatioBasedSamplerOptions options)
    {
        if (_configuration == null)
        {
            return;
        }

        var section = _configuration.GetSection(RatioBasedSamplerKey);
        if (!section.Exists())
        {
            return;
        }

        var parsedOptions = section.Get<RatioBasedSamplerOptions>();
        if (parsedOptions is null)
        {
            return;
        }

        options.Rules.AddRange(parsedOptions.Rules);
    }
}
