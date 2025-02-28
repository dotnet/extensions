// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.AI;

internal sealed class OllamaFunctionTool
{
    public required string Name { get; set; }
    public required string Description { get; set; }
    public required OllamaFunctionToolParameters Parameters { get; set; }
}
