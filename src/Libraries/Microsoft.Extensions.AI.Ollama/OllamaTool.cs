// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.AI;

internal sealed class OllamaTool
{
    public required string Type { get; set; }
    public required OllamaFunctionTool Function { get; set; }
}
