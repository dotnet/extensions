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

[ProviderAlias("R9")]
[Experimental("EXTEXP0003", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
public sealed class LoggerProvider : BaseProvider, ILoggerProvider, IDisposable, ISupportExternalScope
{
    public void SetScopeProvider(IExternalScopeProvider scopeProvider);
    public ILogger CreateLogger(string categoryName);
    protected override void Dispose(bool disposing);
}
