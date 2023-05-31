// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
#if NETCOREAPP3_1_OR_GREATER
using System.Net.Http;
#else
using System.Net;
#endif

namespace Microsoft.Extensions.Http.Telemetry.Tracing.Internal;

internal static class ActivityHelper
{
#if NETCOREAPP3_1_OR_GREATER
    public static HttpRequestMessage? GetRequest(this Activity activity)
    {
        return (HttpRequestMessage?)activity.GetCustomProperty(Constants.CustomPropertyHttpRequestMessage);
    }

    public static HttpResponseMessage? GetResponse(this Activity activity)
    {
        return (HttpResponseMessage?)activity.GetCustomProperty(Constants.CustomPropertyHttpResponseMessage);
    }

    public static void SetRequest(this Activity activity, HttpRequestMessage request)
    {
        activity.SetCustomProperty(Constants.CustomPropertyHttpRequestMessage, request);
    }

    public static void SetResponse(this Activity activity, HttpResponseMessage response)
    {
        activity.SetCustomProperty(Constants.CustomPropertyHttpResponseMessage, response);
    }
#else
    public static HttpWebRequest? GetRequest(this Activity activity)
    {
        return (HttpWebRequest?)activity.GetCustomProperty(Constants.CustomPropertyHttpRequestMessage);
    }

    public static HttpWebResponse? GetResponse(this Activity activity)
    {
        return (HttpWebResponse?)activity.GetCustomProperty(Constants.CustomPropertyHttpResponseMessage);
    }

    public static void SetRequest(this Activity activity, HttpWebRequest request)
    {
        activity.SetCustomProperty(Constants.CustomPropertyHttpRequestMessage, request);
    }

    public static void SetResponse(this Activity activity, HttpWebResponse response)
    {
        activity.SetCustomProperty(Constants.CustomPropertyHttpResponseMessage, response);
    }
#endif

    public static void ClearRequest(this Activity activity)
    {
        activity.SetCustomProperty(Constants.CustomPropertyHttpRequestMessage, null);
    }

    public static void ClearResponse(this Activity activity)
    {
        activity.SetCustomProperty(Constants.CustomPropertyHttpResponseMessage, null);
    }
}
