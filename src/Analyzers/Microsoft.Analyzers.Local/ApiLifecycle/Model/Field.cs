// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.LocalAnalyzers.ApiLifecycle.Model;

internal sealed class Field
{
    public Stage Stage { get; set; } = Stage.Experimental;

    public string Member { get; set; } = string.Empty;

    public override string ToString() => $"{Member}:{Stage}";
}
