// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Compliance.Classification;
using Microsoft.Extensions.Compliance.Testing;

namespace Microsoft.AspNetCore.Diagnostics.Logging.Test;

public class MixedRoutingController : Controller
{
    public IActionResult ConventionalRouting() => Ok();

    [HttpGet("mixed/attribute-routing-1/{param:int:min(1)}")]
    public IActionResult AttributeRoutingWithConstraint([PrivateData] int param) => Ok(param);

    [HttpGet("mixed/attribute-routing-2/{param?}")]
    public IActionResult AttributeRoutingWithNullableConstraint([PrivateData] int? param) => Ok(param);

    [HttpGet("mixed/attribute-routing-3/{param=all}")]
    public IActionResult AttributeRoutingWithDefaultValue([NoDataClassification] string param) => Ok(param);

    [HttpGet("mixed/attribute-routing-4/{param=all}")]
    public IActionResult AttributeRoutingWithUnclassifiedParam(string param) => Ok(param);
}
