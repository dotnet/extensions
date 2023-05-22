// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Primitives;

#pragma warning disable CA2227 // Collection properties should be read only

namespace Microsoft.AspNetCore.HeaderParsing;

/// <summary>
/// Options for the header parsing infrastructure.
/// </summary>
public class HeaderParsingOptions
{
    /// <summary>
    /// Gets or sets default header values for when the given headers aren't present.
    /// </summary>
    /// <remarks>
    /// The keys represent the header name.
    /// </remarks>
    public IDictionary<string, StringValues> DefaultValues { get; set; } = new Dictionary<string, StringValues>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets or sets the default number of cached values per cacheable header.
    /// </summary>
    /// <remarks>
    /// Default value is 128 values.
    /// The number of cached values can be overridden for specific headers using the <see cref="MaxCachedValuesPerHeader" /> property.
    /// </remarks>
    [Range(0, int.MaxValue)]
    public int DefaultMaxCachedValuesPerHeader { get; set; } = 128;

    /// <summary>
    /// Gets or sets the maximum number of cached values for specific headers.
    /// </summary>
    /// <remarks>
    /// The keys represent the header name.
    /// </remarks>
    public IDictionary<string, int> MaxCachedValuesPerHeader { get; set; } = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
}
