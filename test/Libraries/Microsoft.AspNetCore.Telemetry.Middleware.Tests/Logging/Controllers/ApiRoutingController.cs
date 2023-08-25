// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if NET8_0_OR_GREATER

using System;
using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Compliance.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

namespace Microsoft.AspNetCore.Telemetry.Http.Logging.Test.Controllers;

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
        HttpContext.RequestServices.GetRequiredService<FakeTimeProvider>()
            .Advance(TimeSpan.FromMilliseconds(AcceptanceTest.ControllerProcessingTimeMs));

        return "User info...";
    }
}
#endif
