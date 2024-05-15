// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text.Json.Serialization;

namespace Microsoft.Extensions.LocalAnalyzers.ApiLifecycle.Model;

internal sealed class Assembly
{
    public static readonly Assembly Empty = new();

    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("Types")]
    public TypeDef[] Types { get; set; } = Array.Empty<TypeDef>();

    public override string ToString() => Name;
}
