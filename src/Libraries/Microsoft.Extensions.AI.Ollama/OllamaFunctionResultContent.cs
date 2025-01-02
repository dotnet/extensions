// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;

namespace Microsoft.Extensions.AI;

internal sealed class OllamaFunctionResultContent
{
    public string? CallId { get; set; }
    public JsonElement Result { get; set; }
}
