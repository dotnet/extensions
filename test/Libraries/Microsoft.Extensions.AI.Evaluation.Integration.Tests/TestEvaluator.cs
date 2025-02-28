// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit.Sdk;

namespace Microsoft.Extensions.AI.Evaluation.Integration.Tests;

internal class TestEvaluator : IEvaluator
{
    public IReadOnlyList<EvaluationMetric> TestMetrics { get; set; } = [];

    public bool ThrowOnEvaluate { get; set; }

    IReadOnlyCollection<string> IEvaluator.EvaluationMetricNames
        => [.. TestMetrics.Select(t => t.Name)];

    private ValueTask<EvaluationResult> GetResultAsync() =>
        ThrowOnEvaluate
            ? throw FailException.ForFailure("Evaluation failed.")
            : new ValueTask<EvaluationResult>(new EvaluationResult(TestMetrics));

    async ValueTask<EvaluationResult> IEvaluator.EvaluateAsync(
        IEnumerable<ChatMessage> messages,
        ChatMessage modelResponse,
        ChatConfiguration? chatConfiguration,
        IEnumerable<EvaluationContext>? additionalContext,
        CancellationToken cancellationToken)
            => await GetResultAsync();
}
