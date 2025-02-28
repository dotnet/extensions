// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.Extensions.AI;

internal sealed class OllamaFunctionToolParameter
{
    public string? Type { get; set; }
    public string? Description { get; set; }
    public IEnumerable<string>? Enum { get; set; }
}
