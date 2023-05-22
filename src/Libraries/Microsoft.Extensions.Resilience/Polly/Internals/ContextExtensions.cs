// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Polly;

namespace Microsoft.Extensions.Resilience.Internal;

internal static class ContextExtensions
{
    public static string GetPolicyPipelineName(this Context context)
    {
        // Stryker disable once all: https://domoreexp.visualstudio.com/R9/_workitems/edit/2804465
        return context.PolicyWrapKey ?? context.PolicyKey ?? string.Empty;
    }
}
