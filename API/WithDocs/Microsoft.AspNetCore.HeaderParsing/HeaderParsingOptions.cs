// Assembly 'Microsoft.AspNetCore.HeaderParsing'

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Primitives;

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
    public IDictionary<string, StringValues> DefaultValues { get; set; }

    /// <summary>
    /// Gets or sets the default number of cached values per cacheable header.
    /// </summary>
    /// <remarks>
    /// Default value is 128 values.
    /// The number of cached values can be overridden for specific headers using the <see cref="P:Microsoft.AspNetCore.HeaderParsing.HeaderParsingOptions.MaxCachedValuesPerHeader" /> property.
    /// </remarks>
    [Range(0, int.MaxValue)]
    public int DefaultMaxCachedValuesPerHeader { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of cached values for specific headers.
    /// </summary>
    /// <remarks>
    /// The keys represent the header name.
    /// </remarks>
    public IDictionary<string, int> MaxCachedValuesPerHeader { get; set; }

    public HeaderParsingOptions();
}
