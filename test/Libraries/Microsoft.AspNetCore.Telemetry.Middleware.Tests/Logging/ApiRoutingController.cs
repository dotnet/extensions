// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Compliance.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

namespace Microsoft.AspNetCore.Diagnostics.Logging.Test.Controllers;

[ApiController]
[Route("api/users")]
public class ApiRoutingController : ControllerBase
{
    [HttpGet("{userId}/{noDataClassification}")]
    public ActionResult<string> GetUser(
        [FromRoute][PrivateData] string userId,
        [FromRoute] string noDataClassification,
        [FromQuery] string noRedaction)
    {
        Debug.Assert(userId != null, "Test");
        Debug.Assert(noDataClassification != null, "Test");
        Debug.Assert(noRedaction != null, "Test");

        // Request processing imitation:
        var middleware = HttpContext.RequestServices.GetRequiredService<HttpLoggingMiddleware>();
        var fakeTimeProvider = (FakeTimeProvider)middleware.TimeProvider;
        fakeTimeProvider.Advance(TimeSpan.FromMilliseconds(AcceptanceTest.ControllerProcessingTimeMs));

        return "User info...";
    }
}
