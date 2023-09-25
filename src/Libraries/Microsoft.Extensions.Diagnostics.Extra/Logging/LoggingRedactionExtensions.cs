// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Logging;

/// <summary>
/// Extensions for configuring logging redaction features.
/// </summary>
public static class LoggingRedactionExtensions
{
    /// <summary>
    /// Enables redaction functionality within the logging infrastructure.
    /// </summary>
    /// <param name="builder">The dependency injection container to add logging to.</param>
    /// <returns>The value of <paramref name="builder"/>.</returns>
    public static ILoggingBuilder EnableRedaction(this ILoggingBuilder builder)
    {
        _ = Throw.IfNull(builder);

        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerFactory, ExtendedLoggerFactory>());
        _ = builder.Services.AddOptions<LoggerRedactionOptions>();

        return builder;
    }
}
