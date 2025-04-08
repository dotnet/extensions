// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.Extensions.AI.Evaluation.Safety;

internal sealed partial class ContentSafetyService
{
    private sealed class UrlConfigurationComparer : IEqualityComparer<ContentSafetyServiceConfiguration>
    {
        internal static UrlConfigurationComparer Instance { get; } = new UrlConfigurationComparer();

        public bool Equals(ContentSafetyServiceConfiguration? first, ContentSafetyServiceConfiguration? second)
        {
            if (first is null && second is null)
            {
                return true;
            }
            else if (first is null || second is null)
            {
                return false;
            }
            else
            {
                return
                    first.SubscriptionId == second.SubscriptionId &&
                    first.ResourceGroupName == second.ResourceGroupName &&
                    first.ProjectName == second.ProjectName;
            }
        }

        public int GetHashCode(ContentSafetyServiceConfiguration obj)
            => HashCode.Combine(obj.SubscriptionId, obj.ResourceGroupName, obj.ProjectName);
    }
}
