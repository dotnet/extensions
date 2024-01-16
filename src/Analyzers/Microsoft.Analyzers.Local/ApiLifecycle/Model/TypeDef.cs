// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.LocalAnalyzers.ApiLifecycle.Model;

internal sealed class TypeDef
{
    public string ModifiersAndName { get; set; } = string.Empty;

    public string[] Constraints { get; set; } = Array.Empty<string>();

    public string[] BaseTypes { get; set; } = Array.Empty<string>();

    public Stage Stage { get; set; } = Stage.Experimental;

    public Method[] Methods { get; set; } = Array.Empty<Method>();

    public Prop[] Properties { get; set; } = Array.Empty<Prop>();

    public Field[] Fields { get; set; } = Array.Empty<Field>();

    public override string ToString() => $"{ModifiersAndName}:{Stage}";
}
