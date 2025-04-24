// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI.Evaluation;

/// <summary>
/// Extension methods for <see cref="EvaluationMetric"/>.
/// </summary>
public static class EvaluationMetricExtensions
{
    /// <summary>
    /// Adds or updates contextual information with the specified <paramref name="name"/> and <paramref name="value"/>
    /// in the supplied <paramref name="metric"/>'s <see cref="EvaluationMetric.Context"/> collection.
    /// </summary>
    /// <param name="metric">The <see cref="EvaluationMetric"/>.</param>
    /// <param name="name">The name for the contextual information to be added or updated.</param>
    /// <param name="value">The contextual information to be added or updated.</param>
    public static void AddOrUpdateContext(this EvaluationMetric metric, string name, params AIContent[] value)
        => metric.AddOrUpdateContext(name, value as IEnumerable<AIContent>);

    /// <summary>
    /// Adds or updates contextual information with the specified <paramref name="name"/> and <paramref name="value"/>
    /// in the supplied <paramref name="metric"/>'s <see cref="EvaluationMetric.Context"/> collection.
    /// </summary>
    /// <param name="metric">The <see cref="EvaluationMetric"/>.</param>
    /// <param name="name">The name for the contextual information to be added or updated.</param>
    /// <param name="value">The contextual information to be added or updated.</param>
    public static void AddOrUpdateContext(this EvaluationMetric metric, string name, IEnumerable<AIContent> value)
    {
        _ = Throw.IfNull(metric);

        metric.Context ??= new Dictionary<string, IList<AIContent>>();
        metric.Context[name] = [.. value];
    }

    /// <summary>
    /// Determines if the supplied <paramref name="metric"/> contains any
    /// <see cref="EvaluationDiagnostic"/> matching the supplied <paramref name="predicate"/>.
    /// </summary>
    /// <param name="metric">The <see cref="EvaluationMetric"/> that is to be inspected.</param>
    /// <param name="predicate">
    /// A predicate that returns <see langword="true"/> if a matching <see cref="EvaluationDiagnostic"/> is found;
    /// <see langword="false"/> otherwise.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the supplied <paramref name="metric"/> contains any
    /// <see cref="EvaluationDiagnostic"/> matching the supplied <paramref name="predicate"/>; <see langword="false"/>
    /// otherwise.
    /// </returns>
    public static bool ContainsDiagnostics(
        this EvaluationMetric metric,
        Func<EvaluationDiagnostic, bool>? predicate = null)
    {
        _ = Throw.IfNull(metric);

        return
            metric.Diagnostics is not null &&
            (predicate is null
                ? metric.Diagnostics.Any()
                : metric.Diagnostics.Any(predicate));
    }

    /// <summary>
    /// Adds the supplied <see cref="EvaluationDiagnostic"/>s to the supplied <see cref="EvaluationMetric"/>'s
    /// <see cref="EvaluationMetric.Diagnostics"/> collection.
    /// </summary>
    /// <param name="metric">The <see cref="EvaluationMetric"/>.</param>
    /// <param name="diagnostics">The <see cref="EvaluationDiagnostic"/>s to be added.</param>
    public static void AddDiagnostics(this EvaluationMetric metric, IEnumerable<EvaluationDiagnostic> diagnostics)
    {
        _ = Throw.IfNull(metric);
        _ = Throw.IfNull(diagnostics);

        if (diagnostics.Any())
        {
            metric.Diagnostics ??= new List<EvaluationDiagnostic>();

            foreach (EvaluationDiagnostic diagnostic in diagnostics)
            {
                metric.Diagnostics.Add(diagnostic);
            }
        }
    }

    /// <summary>
    /// Adds the supplied <see cref="EvaluationDiagnostic"/>s to the supplied <see cref="EvaluationMetric"/>'s
    /// <see cref="EvaluationMetric.Diagnostics"/> collection.
    /// </summary>
    /// <param name="metric">The <see cref="EvaluationMetric"/>.</param>
    /// <param name="diagnostics">The <see cref="EvaluationDiagnostic"/>s to be added.</param>
    public static void AddDiagnostics(this EvaluationMetric metric, params EvaluationDiagnostic[] diagnostics)
        => metric.AddDiagnostics(diagnostics as IEnumerable<EvaluationDiagnostic>);

    /// <summary>
    /// Adds or updates metadata with the specified <paramref name="name"/> and <paramref name="value"/> in the
    /// supplied <paramref name="metric"/>'s <see cref="EvaluationMetric.Metadata"/> collection.
    /// </summary>
    /// <param name="metric">The <see cref="EvaluationMetric"/>.</param>
    /// <param name="name">The name of the metadata.</param>
    /// <param name="value">The value of the metadata.</param>
    public static void AddOrUpdateMetadata(this EvaluationMetric metric, string name, string value)
    {
        _ = Throw.IfNull(metric);

        metric.Metadata ??= new Dictionary<string, string>();
        metric.Metadata[name] = value;
    }

    /// <summary>
    /// Adds or updates the supplied <paramref name="metadata"/> in the supplied <paramref name="metric"/>'s
    /// <see cref="EvaluationMetric.Metadata"/> collection.
    /// </summary>
    /// <param name="metric">The <see cref="EvaluationMetric"/>.</param>
    /// <param name="metadata">The metadata to be added or updated.</param>
    public static void AddOrUpdateMetadata(this EvaluationMetric metric, IDictionary<string, string> metadata)
    {
        _ = Throw.IfNull(metric);
        _ = Throw.IfNull(metadata);

        foreach (KeyValuePair<string, string> item in metadata)
        {
            metric.AddOrUpdateMetadata(item.Key, item.Value);
        }
    }
}
