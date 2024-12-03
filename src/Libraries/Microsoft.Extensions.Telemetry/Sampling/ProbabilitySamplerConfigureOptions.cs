// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Diagnostics.Sampling;

internal sealed class ProbabilitySamplerConfigureOptions : IConfigureOptions<ProbabilitySamplerOptions>
{
    private const string ProbabilitySamplerKey = "ProbabilitySampler";
    private readonly IConfiguration _configuration;

    public ProbabilitySamplerConfigureOptions(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public void Configure(ProbabilitySamplerOptions options)
    {
        if (_configuration == null)
        {
            return;
        }

        var section = _configuration.GetSection(ProbabilitySamplerKey);
        if (!section.Exists())
        {
            return;
        }

        var parsedOptions = section.Get<ProbabilitySamplerOptions>();
        if (parsedOptions is null)
        {
            return;
        }

        options.Rules.AddRange(parsedOptions.Rules);
    }
}
