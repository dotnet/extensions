// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI.Evaluation;

/// <summary>
/// A collection of one or more <see cref="EvaluationMetric"/>s that represent the result of an evaluation.
/// </summary>
public sealed class EvaluationResult
{
    /// <summary>
    /// Gets or sets a collection of one or more <see cref="EvaluationMetric"/>s that represent the result of an
    /// evaluation.
    /// </summary>
#pragma warning disable CA2227
    // CA2227: Collection properties should be read only.
    // We disable this warning because we want this type to be fully mutable for serialization purposes and for general
    // convenience.
    public IDictionary<string, EvaluationMetric> Metrics { get; set; }
#pragma warning restore CA2227

    /// <summary>
    /// Initializes a new instance of the <see cref="EvaluationResult"/> class.
    /// </summary>
    /// <param name="metrics">
    /// <para>
    /// A dictionary containing one or more <see cref="EvaluationMetric"/>s that represent the result of an evaluation.
    /// </para>
    /// <para>
    /// The dictionary is keyed on the <see cref="EvaluationMetric.Name"/>s of the contained
    /// <see cref="EvaluationMetric"/>s.
    /// </para>
    /// </param>
    [JsonConstructor]
    public EvaluationResult(IDictionary<string, EvaluationMetric> metrics)
    {
        Metrics = metrics;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EvaluationResult"/> class.
    /// </summary>
    /// <param name="metrics">
    /// An enumeration of <see cref="EvaluationMetric"/>s that represent the result of an evaluation.
    /// </param>
    public EvaluationResult(IEnumerable<EvaluationMetric> metrics)
    {
        _ = Throw.IfNull(metrics, nameof(metrics));

        var metricsDictionary = new Dictionary<string, EvaluationMetric>();

        foreach (EvaluationMetric metric in metrics)
        {
#if NET
            if (!metricsDictionary.TryAdd(metric.Name, metric))
            {
                Throw.ArgumentException(nameof(metrics), $"Cannot add multiple metrics with name '{metric.Name}'.");
            }
#else
            if (metricsDictionary.ContainsKey(metric.Name))
            {
                Throw.ArgumentException(nameof(metrics), $"Cannot add multiple metrics with name '{metric.Name}'.");
            }

            metricsDictionary[metric.Name] = metric;
#endif
        }

        Metrics = metricsDictionary;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EvaluationResult"/> class.
    /// </summary>
    /// <param name="metrics">
    /// An array of <see cref="EvaluationMetric"/>s that represent the result of an evaluation.
    /// </param>
    public EvaluationResult(params EvaluationMetric[] metrics)
        : this(metrics as IEnumerable<EvaluationMetric>)
    {
    }

    /// <summary>
    /// Returns an <see cref="EvaluationMetric"/> with type <typeparamref name="T"/> and with the
    /// <see cref="EvaluationMetric.Name"/> specified via <paramref name="metricName"/> if it exists in
    /// <see cref="Metrics"/>. 
    /// </summary>
    /// <typeparam name="T">The type of the <see cref="EvaluationMetric"/> to be returned.</typeparam>
    /// <param name="metricName">
    /// The <see cref="EvaluationMetric.Name"/> of the <see cref="EvaluationMetric"/> to be returned.
    /// </param>
    /// <param name="value">
    /// An <see cref="EvaluationMetric"/> with type <typeparamref name="T"/> and with the
    /// <see cref="EvaluationMetric.Name"/> specified via <paramref name="metricName"/> if it exists in
    /// <see cref="Metrics"/>; <see langword="null"/> otherwise.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if a matching <paramref name="value"/> exists in <see cref="Metrics"/>;
    /// <see langword="false"/> otherwise.
    /// </returns>
    public bool TryGet<T>(string metricName, [NotNullWhen(true)] out T? value)
        where T : EvaluationMetric
    {
        if (Metrics.TryGetValue(metricName, out EvaluationMetric? m) && m is T metric)
        {
            value = metric;
            return true;
        }

        value = default;
        return false;
    }

    /// <summary>
    /// Returns an <see cref="EvaluationMetric"/> with type <typeparamref name="T"/> and with the
    /// <see cref="EvaluationMetric.Name"/> specified via <paramref name="metricName"/> if it exists in
    /// <see cref="Metrics"/>. 
    /// </summary>
    /// <typeparam name="T">The type of the <see cref="EvaluationMetric"/> to be returned.</typeparam>
    /// <param name="metricName">
    /// The <see cref="EvaluationMetric.Name"/> of the <see cref="EvaluationMetric"/> to be returned.
    /// </param>
    /// <returns>
    /// An <see cref="EvaluationMetric"/> with type <typeparamref name="T"/> and with the
    /// <see cref="EvaluationMetric.Name"/> specified via <paramref name="metricName"/> if it exists in
    /// <see cref="Metrics"/>.
    /// </returns>
    /// <exception cref="KeyNotFoundException">
    /// An <see cref="EvaluationMetric"/> with type <typeparamref name="T"/> and with the
    /// <see cref="EvaluationMetric.Name"/> specified via <paramref name="metricName"/> does not exist in
    /// <see cref="Metrics"/>.
    /// </exception>
    public T Get<T>(string metricName)
        where T : EvaluationMetric
    {
        if (Metrics.TryGetValue(metricName, out EvaluationMetric? m) && m is T metric)
        {
            return metric;
        }

        throw new KeyNotFoundException($"Metric '{metricName}' of type '{typeof(T).FullName}' was not found.");
    }
}
