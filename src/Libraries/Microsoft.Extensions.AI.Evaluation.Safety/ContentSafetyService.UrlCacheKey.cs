// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.AI.Evaluation.Safety;

internal sealed partial class ContentSafetyService
{
    private sealed class UrlCacheKey(ContentSafetyServiceConfiguration configuration, string annotationTask)
        : IEquatable<UrlCacheKey>
    {
        internal ContentSafetyServiceConfiguration Configuration { get; } = configuration;
        internal string AnnotationTask { get; } = annotationTask;

        public bool Equals(UrlCacheKey? other) =>
            other is not null &&
            other.Configuration.SubscriptionId == Configuration.SubscriptionId &&
            other.Configuration.ResourceGroupName == Configuration.ResourceGroupName &&
            other.Configuration.ProjectName == Configuration.ProjectName &&
            other.Configuration.Endpoint == Configuration.Endpoint &&
            other.AnnotationTask == AnnotationTask;

        public override bool Equals(object? other) =>
            other is UrlCacheKey otherKey && Equals(otherKey);

        public override int GetHashCode() =>
            HashCode.Combine(
                Configuration.SubscriptionId,
                Configuration.ResourceGroupName,
                Configuration.ProjectName,
                Configuration.Endpoint,
                AnnotationTask);
    }
}
