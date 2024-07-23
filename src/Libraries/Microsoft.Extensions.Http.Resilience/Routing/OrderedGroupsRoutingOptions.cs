// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Http.Resilience;

/// <summary>
/// Represents the options for collection of endpoint groups that have fixed order.
/// </summary>
/// <remarks>
/// This strategy picks the endpoint groups in he same order as they are specified in the <see cref="Groups"/> collection.
/// </remarks>
public class OrderedGroupsRoutingOptions
{
    /// <summary>
    /// Gets or sets the collection of ordered endpoints groups.
    /// </summary>
#pragma warning disable CA2227 // Collection properties should be read only
    [Required]
#if NET8_0_OR_GREATER
    [System.ComponentModel.DataAnnotations.Length(1, int.MaxValue)]
#else
    [Microsoft.Shared.Data.Validation.Length(1)]
#endif
    [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "TODO")]
    [ValidateEnumeratedItems]
    public IList<UriEndpointGroup> Groups { get; set; } = new List<UriEndpointGroup>();
#pragma warning restore CA2227 // Collection properties should be read only
}
