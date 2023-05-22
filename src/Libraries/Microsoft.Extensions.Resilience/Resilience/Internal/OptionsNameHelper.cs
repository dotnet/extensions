// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Resilience.Internal;
internal static class OptionsNameHelper
{
    public static string GetPolicyOptionsName(this SupportedPolicies type, string pipelineName, string policyName) => $"{pipelineName}-{type}-{policyName}";
}
