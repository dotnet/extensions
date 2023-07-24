// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.LocalAnalyzers.Json;

namespace Microsoft.Extensions.LocalAnalyzers.ApiLifecycle.Model;

internal sealed class Assembly
{
    public static readonly Assembly Empty = new();

    public string Name { get; }
    public TypeDef[] Types { get; }

    public Assembly(JsonObject value)
    {
        Name = value[nameof(Name)].AsString ?? string.Empty;
        Types = value.GetValueArray<TypeDef>(nameof(Types));
    }

    private Assembly()
    {
        Name = string.Empty;
        Types = Array.Empty<TypeDef>();
    }

    public override string ToString() => Name;
}
