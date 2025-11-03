// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Microsoft.Extensions.AI.Evaluation.Reporting.Formats;

[method: JsonConstructor]
internal sealed class Dataset(
    IList<ScenarioRunResult> scenarioRunResults,
    DateTime createdAt,
    string? generatorVersion)
{
    public IList<ScenarioRunResult> ScenarioRunResults { get; } = scenarioRunResults;
    public DateTime CreatedAt { get; } = createdAt;
    public string? GeneratorVersion { get; } = generatorVersion;
}
