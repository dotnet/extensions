// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Telemetry;

internal static class Constants
{
    public const string AttributeHttpMethod = "http.method";
    public const string AttributeHttpPath = "http.path";
    public const string AttributeHttpRoute = "http.route";
    public const string AttributeHttpStatusCode = "http.status_code";
    public const string AttributeHttpTarget = "http.target";
    public const string AttributeHttpUrl = "http.url";
    public const string AttributeHttpHost = "http.host";
    public const string AttributeHttpScheme = "http.scheme";
    public const string AttributeHttpFlavor = "http.flavor";
    public const string AttributeNetHostName = "net.host.name";
    public const string AttributeNetHostPort = "net.host.port";
    public const string AttributeUserAgent = "http.user_agent";
    public const string AttributeExceptionType = "env_ex_type";
    public const string AttributeExceptionMessage = "env_ex_msg";
    public const string CustomPropertyHttpRequest = "Tracing.CustomProperty.HttpRequest";
    public const string ActivityStartEvent = "OnStartActivity";
    public const string ActivityExceptionEvent = "OnException";
    public const string RequestNameUnknown = "UnknownRequest";
    public const string OtelStatusCode = "otel.status_code";
}
