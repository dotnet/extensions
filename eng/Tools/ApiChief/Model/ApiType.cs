// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ApiChief.Model;

internal sealed class ApiType : IEquatable<ApiType>
{
    [JsonIgnore]
    public string FullTypeName { get; set; } = string.Empty;

    [JsonPropertyOrder(0)]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyOrder(1)]
    public ApiStage Stage { get; set; }

    [JsonPropertyOrder(2)]
    public ISet<ApiMember>? Methods;

    [JsonPropertyOrder(3)]
    public ISet<ApiMember>? Fields;

    [JsonPropertyOrder(4)]
    public ISet<ApiMember>? Properties;

    public bool Equals(ApiType? other) => other != null && Type == other.Type;
    public override bool Equals(object? obj) => Equals(obj as ApiType);
    public override int GetHashCode() => Type.GetHashCode();
}
