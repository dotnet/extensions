// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.AI.Evaluation.Utilities;

internal static class BuiltInEvaluatorUtilities
{
    internal const string BuiltInEvalMetadataName = "built-in-eval";

    internal static void MarkAsBuiltIn(this EvaluationMetric metric) =>
        metric.AddOrUpdateMetadata(BuiltInEvalMetadataName, "True");

    internal static bool IsBuiltIn(this EvaluationMetric metric) =>
        metric.Metadata?.TryGetValue(BuiltInEvalMetadataName, out string? stringValue) is true &&
        bool.TryParse(stringValue, out bool value) &&
        value;
}
