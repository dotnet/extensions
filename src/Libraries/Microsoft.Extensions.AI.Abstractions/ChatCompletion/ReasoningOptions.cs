// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.DiagnosticIds;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Represents options for configuring reasoning behavior in chat requests.
/// </summary>
/// <remarks>
/// <para>
/// Reasoning options allow control over how much computational effort the model
/// should put into reasoning about the response, and how that reasoning should
/// be exposed to the caller.
/// </para>
/// <para>
/// Not all providers support all reasoning options. Implementations should
/// make a best-effort attempt to map the requested options to the provider's
/// capabilities. If a provider doesn't support reasoning, these options may be ignored.
/// </para>
/// </remarks>
[Experimental(DiagnosticIds.Experiments.AIReasoning, UrlFormat = DiagnosticIds.UrlFormat)]
public sealed class ReasoningOptions
{
    /// <summary>
    /// Gets or sets the level of reasoning effort to apply.
    /// </summary>
    /// <value>
    /// The reasoning effort level, or <see langword="null"/> to use the provider's default.
    /// </value>
    public ReasoningEffort? Effort { get; set; }

    /// <summary>
    /// Gets or sets how reasoning content should be included in the response.
    /// </summary>
    /// <value>
    /// The reasoning output mode, or <see langword="null"/> to use the provider's default.
    /// </value>
    public ReasoningOutput? Output { get; set; }
}
