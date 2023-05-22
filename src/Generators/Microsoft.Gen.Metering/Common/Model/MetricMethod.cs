// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.Gen.Metering.Model;

internal sealed class MetricMethod
{
    public readonly List<MetricParameter> AllParameters = new();
    public HashSet<string> DimensionsKeys = new();
    public string? Name;
    public string? MetricName;
    public bool IsExtensionMethod;
    public string Modifiers = string.Empty;
    public string MetricTypeModifiers = string.Empty;
    public string MetricTypeName = string.Empty;
    public InstrumentKind InstrumentKind;
    public string GenericType = string.Empty;
    public List<StrongTypeConfig> StrongTypeConfigs = new(); // Used for strong type creation only
    public string? StrongTypeObjectName; // Used for strong type creation only
    public bool IsDimensionTypeClass; // Used for strong type creation only
}
