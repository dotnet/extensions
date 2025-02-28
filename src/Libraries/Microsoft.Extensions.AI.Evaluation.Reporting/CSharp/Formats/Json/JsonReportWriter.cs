// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.AI.Evaluation.Reporting.JsonSerialization;

namespace Microsoft.Extensions.AI.Evaluation.Reporting.Formats.Json;

/// <summary>
/// An <see cref="IEvaluationReportWriter"/> that generates a JSON report containing all the
/// <see cref="EvaluationMetric"/>s present in the supplied <see cref="ScenarioRunResult"/>s and writes it to the
/// specified <paramref name="reportFilePath"/>.
/// </summary>
/// <param name="reportFilePath">
/// The path to a file where the report will be written. If the file already exists, it will be overwritten.
/// </param>
public sealed class JsonReportWriter(string reportFilePath) : IEvaluationReportWriter
{
    /// <inheritdoc/>
    public async ValueTask WriteReportAsync(
        IEnumerable<ScenarioRunResult> scenarioRunResults,
        CancellationToken cancellationToken = default)
    {
        var dataset =
            new Dataset(
                scenarioRunResults.ToList(),
                createdAt: DateTime.UtcNow,
                generatorVersion: Constants.Version);

        using var stream =
            new FileStream(
                reportFilePath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 4096,
                useAsync: true);

        await JsonSerializer.SerializeAsync(
            stream,
            dataset,
            SerializerContext.Default.Dataset,
            cancellationToken).ConfigureAwait(false);
    }
}
