// Assembly 'Microsoft.Extensions.Telemetry.Abstractions'

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.Telemetry.Logging;

[AttributeUsage(AttributeTargets.Method)]
[Conditional("CODE_GENERATION_ATTRIBUTES")]
public sealed class LogMethodAttribute : Attribute
{
    public int EventId { get; }
    public string? EventName { get; set; }
    public LogLevel? Level { get; }
    public string Message { get; }
    public bool SkipEnabledCheck { get; set; }
    public LogMethodAttribute(int eventId, LogLevel level, string message);
    public LogMethodAttribute(int eventId, LogLevel level);
    public LogMethodAttribute(LogLevel level, string message);
    public LogMethodAttribute(LogLevel level);
    public LogMethodAttribute(string message);
    public LogMethodAttribute(int eventId, string message);
    public LogMethodAttribute(int eventId);
    [Experimental("EXTEXP0003", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
    public LogMethodAttribute();
}
