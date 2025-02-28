// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.AI;

internal sealed class OllamaEmbeddingRequest
{
    public required string Model { get; set; }
    public required string[] Input { get; set; }
    public OllamaRequestOptions? Options { get; set; }
    public bool? Truncate { get; set; }
    public long? KeepAlive { get; set; }
}
