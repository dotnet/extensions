// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Http.Telemetry.Tracing;

internal static class Constants
{
    public const string AttributeHttpPath = "http.path";
    public const string AttributeHttpRoute = "http.route";
    public const string AttributeHttpTarget = "http.target";
    public const string AttributeHttpUrl = "http.url";
    public const string AttributeHttpScheme = "http.scheme";
    public const string AttributeHttpFlavor = "http.flavor";
    public const string AttributeHttpHost = "http.host";
    public const string AttributeNetPeerName = "net.peer.name";
    public const string AttributeNetPeerPort = "net.peer.port";
    public const string AttributeUserAgent = "http.user_agent";
    public const string CustomPropertyHttpRequestMessage = "Tracing.CustomProperty.HttpRequestMessage";
    public const string CustomPropertyHttpResponseMessage = "Tracing.CustomProperty.HttpResponseMessage";
    public const string ActivityStartEvent = "OnStartActivity";
    public const string ActivityStopEvent = "OnStopActivity";
}
