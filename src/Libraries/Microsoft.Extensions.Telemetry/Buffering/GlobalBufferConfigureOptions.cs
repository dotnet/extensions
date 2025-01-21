﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Diagnostics.Buffering;

internal sealed class GlobalBufferConfigureOptions : IConfigureOptions<GlobalBufferOptions>
{
    private const string BufferingKey = "Buffering";
    private readonly IConfiguration _configuration;

    public GlobalBufferConfigureOptions(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public void Configure(GlobalBufferOptions options)
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

        var parsedOptions = section.Get<GlobalBufferOptions>();
        if (parsedOptions is null)
        {
            return;
        }

        if (parsedOptions.MaxLogRecordSizeInBytes > 0)
        {
            options.MaxLogRecordSizeInBytes = parsedOptions.MaxLogRecordSizeInBytes;
        }

        if (parsedOptions.BufferSizeInBytes > 0)
        {
            options.BufferSizeInBytes = parsedOptions.BufferSizeInBytes;
        }

        options.Rules.AddRange(parsedOptions.Rules);
    }
}
