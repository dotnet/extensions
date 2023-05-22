// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.Http.Telemetry.Logging.Test.Internal;

internal static class TestConfiguration
{
    /// <summary>
    /// It returns section with request body read timeout set to specified value.
    /// </summary>
    /// <param name="timeout">Timeout to be set for request body read timeout.</param>
    /// <returns>Instance of configuration section.</returns>
    public static IConfigurationSection GetHttpClientLoggingConfigurationSection(TimeSpan timeout) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                {
                    $"{nameof(LoggingOptions)}:{nameof(LoggingOptions.BodyReadTimeout)}",
                    timeout.ToString()
                }
            })
            .Build()
            .GetSection($"{nameof(LoggingOptions)}");
}
