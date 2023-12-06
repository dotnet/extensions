// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel.DataAnnotations;

namespace Microsoft.Extensions.Http.Resilience;

#pragma warning disable IDE0032 // Use auto property

/// <summary>
/// Represents a URI-based endpoint.
/// </summary>
public class UriEndpoint
{
    private Uri? _uri;

    /// <summary>
    /// Gets or sets the URL of the endpoint.
    /// </summary>
    /// <remarks>
    /// Only schema, domain name, and port are used. The rest of the URL is constructed from the request URL.
    /// </remarks>
    [Required]
    public Uri? Uri
    {
        get => _uri;
        set => _uri = value;
    }
}
