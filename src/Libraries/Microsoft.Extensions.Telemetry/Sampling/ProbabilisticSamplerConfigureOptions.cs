// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Diagnostics.Sampling;

internal sealed class ProbabilisticSamplerConfigureOptions : IConfigureOptions<ProbabilisticSamplerOptions>
{
    private const string ProbabilisticSamplerKey = "ProbabilisticSampler";
    private readonly IConfiguration _configuration;

    public ProbabilisticSamplerConfigureOptions(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public void Configure(ProbabilisticSamplerOptions options)
    {
        if (_configuration == null)
        {
            return;
        }

        var section = _configuration.GetSection(ProbabilisticSamplerKey);
        if (!section.Exists())
        {
            return;
        }

        var parsedOptions = section.Get<ProbabilisticSamplerOptions>();
        if (parsedOptions is null)
        {
            return;
        }

        options.Rules.AddRange(parsedOptions.Rules);
    }
}
