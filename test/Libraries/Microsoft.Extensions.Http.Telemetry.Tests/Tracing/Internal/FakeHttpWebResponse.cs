// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net;
using System.Runtime.Serialization;

namespace Microsoft.Extensions.Http.Telemetry.Tracing.Test.Internal;

public class FakeHttpWebResponse : HttpWebResponse
{
    public FakeHttpWebResponse(string uri)
#pragma warning disable CS0618 // Type or member is obsolete
        : base(GetSerializationInfo(uri), default)
#pragma warning restore CS0618 // Type or member is obsolete
    {
    }

    private static SerializationInfo GetSerializationInfo(string uri)
    {
#pragma warning disable SYSLIB0050
        var serializationInfo = new SerializationInfo(typeof(HttpWebResponse), new FormatterConverter());
#pragma warning restore SYSLIB0050
        serializationInfo.AddValue("m_HttpResponseHeaders", new WebHeaderCollection(), typeof(WebHeaderCollection));
        serializationInfo.AddValue("m_Uri", new Uri(uri), typeof(Uri));
        serializationInfo.AddValue("m_Certificate", null, typeof(System.Security.Cryptography.X509Certificates.X509Certificate));
        serializationInfo.AddValue("m_Version", new Version(), typeof(Version));
        serializationInfo.AddValue("m_StatusCode", (int)HttpStatusCode.OK);
        serializationInfo.AddValue("m_ContentLength", 0);
        serializationInfo.AddValue("m_Verb", "GET");
        serializationInfo.AddValue("m_StatusDescription", "");
        serializationInfo.AddValue("m_MediaType", "");
        return serializationInfo;
    }
}
