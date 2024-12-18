// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Diagnostics.Buffering;

internal sealed class HttpRequestBufferConfigureOptions : IConfigureOptions<HttpRequestBufferOptions>
{
    private const string BufferingKey = "Buffering";
    private readonly IConfiguration _configuration;

    public HttpRequestBufferConfigureOptions(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public void Configure(HttpRequestBufferOptions options)
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

        var parsedOptions = section.Get<HttpRequestBufferOptions>();
        if (parsedOptions is null)
        {
            return;
        }

        options.Rules.AddRange(parsedOptions.Rules);
    }
}
