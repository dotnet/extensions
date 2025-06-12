// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Text.Json;

namespace Microsoft.Extensions.AI;

internal sealed class OllamaChatRequest
{
    public required string Model { get; set; }
    public required IList<OllamaChatRequestMessage> Messages { get; set; }
    public JsonElement? Format { get; set; }
    public bool Stream { get; set; }
    public IEnumerable<OllamaTool>? Tools { get; set; }
    public OllamaRequestOptions? Options { get; set; }
}
