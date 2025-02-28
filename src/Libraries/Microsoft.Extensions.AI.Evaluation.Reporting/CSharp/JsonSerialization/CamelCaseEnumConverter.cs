// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.Extensions.AI.Evaluation.Reporting.JsonSerialization;

internal sealed class CamelCaseEnumConverter<TEnum>() :
    JsonStringEnumConverter<TEnum>(JsonNamingPolicy.CamelCase)
    where TEnum : struct, System.Enum;
