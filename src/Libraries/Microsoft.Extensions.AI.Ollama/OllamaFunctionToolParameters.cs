// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Text.Json;

namespace Microsoft.Extensions.AI;

internal sealed class OllamaFunctionToolParameters
{
    public string Type { get; set; } = "object";
    public required IDictionary<string, JsonElement> Properties { get; set; }
    public IList<string>? Required { get; set; }
}
