// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.AI.Evaluation.Utilities;

internal static class BuiltInMetricUtilities
{
    internal const string ModelUsedMetadataName = "model-used";
    internal const string InputTokensUsedMetadataName = "input-tokens-used";
    internal const string OutputTokensUsedMetadataName = "output-tokens-used";
    internal const string TotalTokensUsedMetadataName = "total-tokens-used";
    internal const string DurationInSecondsMetadataName = "duration-in-seconds";
    internal const string BuiltInEvalMetadataName = "built-in-eval";

    internal static void MarkAsBuiltIn(this EvaluationMetric metric) =>
        metric.AddOrUpdateMetadata(name: BuiltInEvalMetadataName, value: bool.TrueString);

    internal static bool IsBuiltIn(this EvaluationMetric metric) =>
        metric.Metadata?.TryGetValue(BuiltInEvalMetadataName, out string? stringValue) is true &&
        bool.TryParse(stringValue, out bool value) &&
        value;
}
