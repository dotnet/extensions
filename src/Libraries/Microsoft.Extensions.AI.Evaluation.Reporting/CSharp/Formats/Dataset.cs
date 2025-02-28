// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable S3604
// S3604: Member initializer values should not be redundant.
// We disable this warning because it is a false positive arising from the analyzer's lack of support for C#'s primary
// constructor syntax.

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
