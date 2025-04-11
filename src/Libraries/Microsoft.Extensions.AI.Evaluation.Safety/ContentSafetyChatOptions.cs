// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable S3604
// S3604: Member initializer values should not be redundant.
// We disable this warning because it is a false positive arising from the analyzer's lack of support for C#'s primary
// constructor syntax.

namespace Microsoft.Extensions.AI.Evaluation.Safety;

internal sealed class ContentSafetyChatOptions(string annotationTask, string evaluatorName) : ChatOptions
{
    internal string AnnotationTask { get; } = annotationTask;
    internal string EvaluatorName { get; } = evaluatorName;
}
