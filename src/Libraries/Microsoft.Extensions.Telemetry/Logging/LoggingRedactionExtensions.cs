// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Telemetry.Logging;

/// <summary>
/// Extensions for configuring logging redaction features.
/// </summary>
[Experimental(diagnosticId: "TBD", UrlFormat = "TBD")]
public static class LoggingRedactionExtensions
{
    /// <summary>
    /// Enables redaction functionality within the logging infrastructure.
    /// </summary>
    /// <param name="builder">The dependency injection container to add logging to.</param>
    /// <returns>The value of <paramref name="builder"/>.</returns>
    public static ILoggingBuilder EnableRedactionAndEnrichment(this ILoggingBuilder builder)
    {
        _ = Throw.IfNull(builder);

        builder.Services.TryAddSingleton<ILoggerFactory, ExtendedLoggerFactory>();
        _ = builder.Services.AddOptions<LoggerRedactionOptions>();

        return builder;
    }
}
