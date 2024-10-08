// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.AI;

internal sealed class OllamaChatResponse
{
    public string? Model { get; set; }
    public string? CreatedAt { get; set; }
    public long? TotalDuration { get; set; }
    public long? LoadDuration { get; set; }
    public string? DoneReason { get; set; }
    public int? PromptEvalCount { get; set; }
    public long? PromptEvalDuration { get; set; }
    public int? EvalCount { get; set; }
    public long? EvalDuration { get; set; }
    public OllamaChatResponseMessage? Message { get; set; }
    public bool Done { get; set; }
    public string? Error { get; set; }
}
