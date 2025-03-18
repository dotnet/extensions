﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#if NET9_0_OR_GREATER

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Diagnostics.Buffering;

internal sealed class GlobalLogBufferingConfigureOptions : IConfigureOptions<GlobalLogBufferingOptions>
{
    private const string ConfigSectionName = "GlobalLogBuffering";
    private readonly IConfiguration _configuration;

    public GlobalLogBufferingConfigureOptions(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public void Configure(GlobalLogBufferingOptions options)
    {
        if (_configuration is null)
        {
            return;
        }

        IConfigurationSection section = _configuration.GetSection(ConfigSectionName);
        if (!section.Exists())
        {
            return;
        }

        var parsedOptions = section.Get<GlobalLogBufferingOptions>();
        if (parsedOptions is null)
        {
            return;
        }

        if (parsedOptions.MaxLogRecordSizeInBytes > 0)
        {
            options.MaxLogRecordSizeInBytes = parsedOptions.MaxLogRecordSizeInBytes;
        }

        if (parsedOptions.MaxBufferSizeInBytes > 0)
        {
            options.MaxBufferSizeInBytes = parsedOptions.MaxBufferSizeInBytes;
        }

        foreach (var rule in parsedOptions.Rules)
        {
            options.Rules.Add(rule);
        }
    }
}
#endif
