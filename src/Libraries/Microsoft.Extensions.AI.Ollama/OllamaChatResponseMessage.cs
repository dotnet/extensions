// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.AI;

internal sealed class OllamaChatResponseMessage
{
    public required string Role { get; set; }
    public required string Content { get; set; }
    public OllamaToolCall[]? ToolCalls { get; set; }
}
