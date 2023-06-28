﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Http.Resilience;

/// <summary>
/// Represents a collection of <see cref="WeightedEndpoint"/>.
/// </summary>
public class EndpointGroup
{
    /// <summary>
    /// Gets or sets the endpoints in this endpoint group.
    /// </summary>
    /// <remarks>
    /// By default the endpoints are initialized with an empty list.
    /// The client must define the endpoint for each endpoint group.
    /// At least one endpoint must be defined on each endpoint group in order to performed hedged requests.
    /// </remarks>
#pragma warning disable CA2227 // Collection properties should be read only
#pragma warning disable IL2026 // Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code
    [Microsoft.Shared.Data.Validation.Length(1)]
    [ValidateEnumeratedItems]
    public IList<WeightedEndpoint> Endpoints { get; set; } = new List<WeightedEndpoint>();
#pragma warning restore IL2026 // Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code
#pragma warning restore CA2227 // Collection properties should be read only
}
