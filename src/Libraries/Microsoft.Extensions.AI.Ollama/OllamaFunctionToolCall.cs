// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.Extensions.AI;

internal sealed class OllamaFunctionToolCall
{
    public required string Name { get; set; }
    public IDictionary<string, object?>? Arguments { get; set; }
}
