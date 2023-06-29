// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel.DataAnnotations;

namespace Microsoft.Extensions.Http.Resilience.Routing.Internal;

internal class RequestRoutingOptions
{
    [Required]
    public Func<RequestRoutingStrategy>? RoutingStrategyProvider { get; set; }
}
