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
        : IEquatable<UrlCacheKey>
    {
        internal ContentSafetyServiceConfiguration Configuration { get; } = configuration;
        internal string AnnotationTask { get; } = annotationTask;

        public bool Equals(UrlCacheKey? other)
        {
            if (other is null)
            {
                return false;
            }
            else
            {
                return
                    other.Configuration.SubscriptionId == Configuration.SubscriptionId &&
                    other.Configuration.ResourceGroupName == Configuration.ResourceGroupName &&
                    other.Configuration.ProjectName == Configuration.ProjectName &&
                    other.AnnotationTask == AnnotationTask;
            }
        }

        public override bool Equals(object? other)
            => other is UrlCacheKey otherKey && Equals(otherKey);

        public override int GetHashCode() =>
            HashCode.Combine(
                Configuration.SubscriptionId,
                Configuration.ResourceGroupName,
                Configuration.ProjectName,
                AnnotationTask);
    }
}
