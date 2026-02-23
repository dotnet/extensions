// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI.Evaluation.Safety;

internal sealed class ContentSafetyChatOptions : ChatOptions
{
    public ContentSafetyChatOptions(string annotationTask, string evaluatorName)
    {
        AnnotationTask = annotationTask;
        EvaluatorName = evaluatorName;
    }

    private ContentSafetyChatOptions(ContentSafetyChatOptions other)
        : base(Throw.IfNull(other))
    {
        AnnotationTask = other.AnnotationTask;
        EvaluatorName = other.EvaluatorName;
    }

    public string AnnotationTask { get; }
    public string EvaluatorName { get; }

    public override ChatOptions Clone() => new ContentSafetyChatOptions(this);
}
