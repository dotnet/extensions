﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.Gen.Metering.Model;

namespace Microsoft.Gen.Metering;

internal sealed class StrongTypeAttributeParameters
{
    public string MetricNameFromAttribute = string.Empty;
    public HashSet<string> DimensionHashSet = new();
    public Dictionary<string, string> DimensionDescriptionDictionary = new();
    public List<StrongTypeConfig> StrongTypeConfigs = new();
    public string StrongTypeObjectName = string.Empty;
    public bool IsClass;
}
