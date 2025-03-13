// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#if NET9_0_OR_GREATER
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Diagnostics.Buffering;

internal sealed class PerIncomingRequestLogBufferingConfigureOptions : IConfigureOptions<PerRequestLogBufferingOptions>
{
    private const string BufferingKey = "Buffering";
    private readonly IConfiguration _configuration;

    public PerIncomingRequestLogBufferingConfigureOptions(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public void Configure(PerRequestLogBufferingOptions options)
    {
        if (_configuration == null)
        {
            return;
        }

        var section = _configuration.GetSection(BufferingKey);
        if (!section.Exists())
        {
            return;
        }

        var parsedOptions = section.Get<PerRequestLogBufferingOptions>();
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
#endif
