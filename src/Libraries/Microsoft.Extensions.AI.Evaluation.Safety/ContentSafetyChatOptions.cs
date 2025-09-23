// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.AI.Evaluation.Safety;

internal sealed class ContentSafetyChatOptions(string annotationTask, string evaluatorName) : ChatOptions
{
    internal string AnnotationTask { get; } = annotationTask;
    internal string EvaluatorName { get; } = evaluatorName;
}
