// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.LocalAnalyzers.Json;

namespace Microsoft.Extensions.LocalAnalyzers.ApiLifecycle.Model;

internal sealed class Method
{
    public Stage Stage { get; }
    public string Member { get; }

    public Method(JsonObject value)
    {
        Member = value[nameof(Member)].AsString ?? string.Empty;

        var enumString = value[nameof(Stage)].AsString;

        if (Enum.TryParse<Stage>(enumString, out var stage))
        {
            Stage = stage;
        }
        else
        {
            Stage = Stage.Experimental;
        }
    }
}
