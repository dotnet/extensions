// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Telemetry.Test.Internal;

public sealed class TestEnricher2 : IHttpTraceEnricher
{
    public void Enrich(Activity activity, HttpRequest request)
    {
        activity.AddTag(Constants.AttributeHttpRoute, "randomRoute");
        activity.AddTag(Constants.AttributeHttpPath, "randomPath");
    }
}
