// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.Logging.AzureWebAppDiagnostics;
using Microsoft.Extensions.Logging.AzureWebAppDiagnostics.Internal;

namespace Microsoft.Extensions.Logging
{
    /// <summary>
    /// Extension methods for <see cref="AzureWebAppDiagnosticsLoggerProvider"/>.
    /// </summary>
    public static class AzureWebAppDiagnosticsFactoryExtensions
    {
        /// <summary>
        /// Adds an Azure Web Apps diagnostics logger.
        /// </summary>
        /// <param name="factory">The extension method argument</param>
        /// <param name="fileSizeLimitMb">A strictly positive value representing the maximum log size in megabytes. Once the log is full, no more message will be appended</param>
        public static ILoggerFactory AddAzureWebAppDiagnostics(this ILoggerFactory factory, int fileSizeLimitMb = FileLoggerProvider.DefaultFileSizeLimitMb)
        {
            if (WebAppContext.Default.IsRunningInAzureWebApp)
            {
                // Only add the provider if we're in Azure WebApp. That cannot change once the apps started
                factory.AddProvider(new AzureWebAppDiagnosticsLoggerProvider(WebAppContext.Default, fileSizeLimitMb));
            }
            return factory;
        }
    }
}
