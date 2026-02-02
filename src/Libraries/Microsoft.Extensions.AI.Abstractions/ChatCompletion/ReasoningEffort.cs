// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.AI;

/// <summary>
/// Specifies the level of reasoning effort that should be applied when generating chat responses.
/// </summary>
/// <remarks>
/// This value controls how much computational effort the model should put into reasoning.
/// Higher values may result in more thoughtful responses but with increased latency and token usage.
/// The specific interpretation and support for each level may vary between providers.
/// </remarks>
public enum ReasoningEffort
{
    /// <summary>
    /// No reasoning effort. Disables reasoning for providers that support it.
    /// </summary>
    None = 0,

    /// <summary>
    /// Low reasoning effort. Minimal reasoning for faster responses.
    /// </summary>
    Low = 1,

    /// <summary>
    /// Medium reasoning effort. Balanced reasoning for most use cases.
    /// </summary>
    Medium = 2,

    /// <summary>
    /// High reasoning effort. Extensive reasoning for complex tasks.
    /// </summary>
    High = 3,

    /// <summary>
    /// Extra high reasoning effort. Maximum reasoning for the most demanding tasks.
    /// </summary>
    ExtraHigh = 4,
}
