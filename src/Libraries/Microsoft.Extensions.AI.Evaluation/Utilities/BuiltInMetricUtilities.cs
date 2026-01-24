// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.AI.Evaluation.Utilities;

internal static class BuiltInMetricUtilities
{
    internal const string EvalModelMetadataName = "eval-model";
    internal const string EvalInputTokensMetadataName = "eval-input-tokens";
    internal const string EvalOutputTokensMetadataName = "eval-output-tokens";
    internal const string EvalTotalTokensMetadataName = "eval-total-tokens";
    internal const string EvalDurationMillisecondsMetadataName = "eval-duration-ms";
    internal const string BuiltInEvalMetadataName = "built-in-eval";

    internal static void MarkAsBuiltIn(this EvaluationMetric metric) =>
        metric.AddOrUpdateMetadata(name: BuiltInEvalMetadataName, value: bool.TrueString);

    internal static bool IsBuiltIn(this EvaluationMetric metric) =>
        metric.Metadata?.TryGetValue(BuiltInEvalMetadataName, out string? stringValue) is true &&
        bool.TryParse(stringValue, out bool value) &&
        value;
}
