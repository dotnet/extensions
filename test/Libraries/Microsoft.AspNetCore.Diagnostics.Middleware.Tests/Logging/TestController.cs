// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if NETCOREAPP3_1_OR_GREATER
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Compliance.Testing;

namespace Microsoft.AspNetCore.Diagnostics.Logging.Test;

[ApiController]
[Route("[controller]")]
public class TestController : ControllerBase
{
    [HttpGet]
    [Route("v1/profile/users/{userId}")]
    [SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "For testing")]
    public async Task<IActionResult> GetTest1Async([PrivateData] string userId)
    {
        await Task.Yield();
        return Ok();
    }

    [HttpGet]
    [Route("v1/profile/users/{userId}/teams/{teamId}")]
    [SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "For testing")]
    public async Task<IActionResult> GetTest2Async([PrivateData] string userId, [PrivateData] string teamId)
    {
        await Task.Yield();
        return Ok();
    }

    [HttpGet]
    [Route("v1/profile/users/{userId}/teams/{teamId}/chats/{chatId}")]
    [SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "For testing")]
    public async Task<IActionResult> GetTest3Async([PrivateData] string userId, [PrivateData] string teamId, string chatId)
    {
        await Task.Yield();
        return Ok();
    }

    [HttpGet]
    [Route("v1/profile/users")]
    public async Task<IActionResult> GetTest4Async()
    {
        await Task.Yield();
        return Ok();
    }

    [HttpGet]
    [Route("v1/profile/users/{userId}/teams/{teamId}/chats/{chatId}")]
    [SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "For testing")]
    public async Task<IActionResult> GetTest5Async(string userId, string teamId, string chatId)
    {
        await Task.Yield();
        return Ok();
    }
}
#endif
