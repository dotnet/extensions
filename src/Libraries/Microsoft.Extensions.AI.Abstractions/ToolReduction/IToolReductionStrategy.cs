// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Shared.DiagnosticIds;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Represents a strategy capable of selecting a reduced set of tools for a chat request.
/// </summary>
/// <remarks>
/// A tool reduction strategy is invoked prior to sending a request to an underlying <see cref="IChatClient"/>,
/// enabling scenarios where a large tool catalog must be trimmed to fit provider limits or to improve model
/// tool selection quality.
/// <para>
/// The implementation should return a non-<see langword="null"/> enumerable. Returning the original
/// <see cref="ChatOptions.Tools"/> instance indicates no change. Returning a different enumerable indicates
/// the caller may replace the existing tool list.
/// </para>
/// </remarks>
[Experimental(diagnosticId: DiagnosticIds.Experiments.ToolReduction, UrlFormat = DiagnosticIds.UrlFormat)]
public interface IToolReductionStrategy
{
    /// <summary>
    /// Selects the tools that should be included for a specific request.
    /// </summary>
    /// <param name="messages">The chat messages for the request. This is an <see cref="IEnumerable{T}"/> to avoid premature materialization.</param>
    /// <param name="options">The chat options for the request (may be <see langword="null"/>).</param>
    /// <param name="cancellationToken">A token to observe cancellation.</param>
    /// <returns>
    /// A (possibly reduced) enumerable of <see cref="AITool"/> instances. Must never be <see langword="null"/>.
    /// Returning the same instance referenced by <paramref name="options"/>.<see cref="ChatOptions.Tools"/> signals no change.
    /// </returns>
    Task<IEnumerable<AITool>> SelectToolsForRequestAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options,
        CancellationToken cancellationToken = default);
}
