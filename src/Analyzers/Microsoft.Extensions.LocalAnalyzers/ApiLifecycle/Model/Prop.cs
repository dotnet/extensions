// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.LocalAnalyzers.Json;

namespace Microsoft.Extensions.LocalAnalyzers.ApiLifecycle.Model;

internal sealed class Prop
{
    public Stage Stage { get; }
    public string Member { get; }

    public Prop(JsonObject value)
    {
        Member = value[nameof(Member)].AsString ?? string.Empty;

        var stageString = value[nameof(Stage)].AsString;

        _ = Enum.TryParse<Stage>(stageString, out var stage);

        Stage = stage;
    }
}
