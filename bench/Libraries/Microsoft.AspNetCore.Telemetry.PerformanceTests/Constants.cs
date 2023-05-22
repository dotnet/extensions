// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Telemetry;

internal static class Constants
{
    public const string AttributeHttpPath = "http.path";
    public const string AttributeHttpRoute = "http.route";
    public const string AttributeHttpTarget = "http.target";
    public const string AttributeHttpUrl = "http.url";
    public const string Redacted = "redacted";
    public const string CustomPropertyHttpRequest = "Tracing.CustomProperty.HttpRequest";
    public const string ActivityStartEvent = "OnStartActivity";
}
