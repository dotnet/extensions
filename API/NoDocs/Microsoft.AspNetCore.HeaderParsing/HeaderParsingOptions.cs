// Assembly 'Microsoft.AspNetCore.HeaderParsing'

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.HeaderParsing;

public class HeaderParsingOptions
{
    public IDictionary<string, StringValues> DefaultValues { get; set; }
    [Range(0, int.MaxValue)]
    public int DefaultMaxCachedValuesPerHeader { get; set; }
    public IDictionary<string, int> MaxCachedValuesPerHeader { get; set; }
    public HeaderParsingOptions();
}
