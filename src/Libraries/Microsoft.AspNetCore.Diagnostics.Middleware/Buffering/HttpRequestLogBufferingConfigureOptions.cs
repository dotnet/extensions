// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Diagnostics.Buffering;

internal sealed class HttpRequestLogBufferingConfigureOptions : IConfigureOptions<HttpRequestLogBufferingOptions>
{
    private const string BufferingKey = "Buffering";
    private readonly IConfiguration _configuration;

    public HttpRequestLogBufferingConfigureOptions(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public void Configure(HttpRequestLogBufferingOptions options)
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

        var parsedOptions = section.Get<HttpRequestLogBufferingOptions>();
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
