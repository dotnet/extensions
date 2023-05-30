// Assembly 'Microsoft.Extensions.Telemetry'

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Telemetry.Enrichment;
using OpenTelemetry;
using OpenTelemetry.Logs;

namespace Microsoft.Extensions.Telemetry.Logging;

/// <summary>
/// OpenTelemetry Logger provider class.
/// </summary>
[ProviderAlias("R9")]
[Experimental("EXTEXP0003", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
public sealed class LoggerProvider : BaseProvider, ILoggerProvider, IDisposable, ISupportExternalScope
{
    /// <summary>
    /// Sets external scope information source for logger provider.
    /// </summary>
    /// <param name="scopeProvider">scope provider object.</param>
    public void SetScopeProvider(IExternalScopeProvider scopeProvider);

    /// <summary>
    /// Creates a new Microsoft.Extensions.Logging.ILogger instance.
    /// </summary>
    /// <param name="categoryName">The category name for message produced by the logger.</param>
    /// <returns>ILogger object.</returns>
    public ILogger CreateLogger(string categoryName);

    /// <summary>
    /// Performs tasks related to freeing up resources.
    /// </summary>
    /// <param name="disposing">Parameter indicating whether resources need disposing.</param>
    protected override void Dispose(bool disposing);
}
