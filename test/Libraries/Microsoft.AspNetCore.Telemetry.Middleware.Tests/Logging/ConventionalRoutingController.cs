// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Compliance.Testing;

namespace Microsoft.AspNetCore.Diagnostics.Logging.Test.Controllers;

public class ConventionalRoutingController : Controller
{
    public const string Route = "{controller=ConventionalRouting}/{action=Index}/{param?}";

    public IActionResult Index() => Ok();

    public IActionResult GetEntity([PrivateData] int param) => Ok(param);

    public IActionResult GetData(int param) => Ok(param);
}
