// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.AI;

/// <summary>
/// Specifies how reasoning content should be included in the response.
/// </summary>
/// <remarks>
/// Some providers support including reasoning or thinking traces in the response.
/// This setting controls whether and how that reasoning content is exposed.
/// </remarks>
public enum ReasoningOutput
{
    /// <summary>
    /// No reasoning output. Do not include reasoning content in the response.
    /// </summary>
    None,

    /// <summary>
    /// Summary reasoning output. Include a summary of the reasoning process.
    /// </summary>
    Summary,

    /// <summary>
    /// Full reasoning output. Include all reasoning content in the response.
    /// </summary>
    Full,
}
