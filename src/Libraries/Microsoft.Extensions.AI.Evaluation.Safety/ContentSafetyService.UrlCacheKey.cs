// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable S3604
// S3604: Member initializer values should not be redundant.
// We disable this warning because it is a false positive arising from the analyzer's lack of support for C#'s primary
// constructor syntax.

using System;

namespace Microsoft.Extensions.AI.Evaluation.Safety;

internal sealed partial class ContentSafetyService
{
    private sealed class UrlCacheKey(ContentSafetyServiceConfiguration configuration, string annotationTask)
    {
        internal ContentSafetyServiceConfiguration Configuration { get; } = configuration;
        internal string AnnotationTask { get; } = annotationTask;

        public override bool Equals(object? other)
        {
            if (other is not UrlCacheKey otherKey)
            {
                return false;
            }
            else
            {
                return
                    otherKey.Configuration.SubscriptionId == Configuration.SubscriptionId &&
                    otherKey.Configuration.ResourceGroupName == Configuration.ResourceGroupName &&
                    otherKey.Configuration.ProjectName == Configuration.ProjectName &&
                    otherKey.AnnotationTask == AnnotationTask;
            }
        }

        public override int GetHashCode() =>
            HashCode.Combine(
                Configuration.SubscriptionId,
                Configuration.ResourceGroupName,
                Configuration.ProjectName,
                AnnotationTask);
    }
}
