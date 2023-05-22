// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.LocalAnalyzers.Json;

namespace Microsoft.Extensions.LocalAnalyzers.ApiLifecycle.Model;

internal sealed class TypeDef
{
    public string ModifiersAndName { get; }
    public string[] Constraints { get; }
    public string[] BaseTypes { get; }
    public Stage Stage { get; }
    public Method[] Methods { get; }
    public Prop[] Properties { get; }
    public Field[] Fields { get; }

    public TypeDef(JsonObject value)
    {
        ModifiersAndName = Utils.StripBaseAndConstraints(value["Type"].AsString ?? string.Empty);
        Constraints = Utils.GetConstraints(value["Type"].AsString ?? string.Empty);
        BaseTypes = Utils.GetBaseTypes(value["Type"].AsString ?? string.Empty);
        _ = Enum.TryParse<Stage>(value[nameof(Stage)].AsString, out var stage);

        Stage = stage;
        Methods = value.GetValueArray<Method>(nameof(Methods));
        Properties = value.GetValueArray<Prop>(nameof(Properties));
        Fields = value.GetValueArray<Field>(nameof(Fields));
    }
}
