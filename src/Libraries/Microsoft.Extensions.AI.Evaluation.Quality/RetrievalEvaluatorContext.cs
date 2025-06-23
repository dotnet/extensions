// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Extensions.AI.Evaluation.Quality;

/// <summary>
/// Contextual information that the <see cref="RetrievalEvaluator"/> uses to evaluate an AI system's performance in
/// retrieving information for additional context.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="RetrievalEvaluator"/> measures the degree to which the information present in the context chunks
/// supplied via <see cref="RetrievedContextChunks"/> are relevant to the user request, and how well these chunks are
/// ranked (with the most relevant information appearing before less relevant information).
/// </para>
/// <para>
/// High retrieval scores indicate that the AI system has successfully extracted and ranked the most relevant
/// information at the top, without introducing bias from external knowledge and ignoring factual correctness.
/// Conversely, low retrieval scores suggest that the AI system has failed to surface the most relevant context chunks
/// at the top of the list and / or introduced bias and ignored factual correctness.
/// </para>
/// </remarks>
public sealed class RetrievalEvaluatorContext : EvaluationContext
{
    /// <summary>
    /// Gets the unique <see cref="EvaluationContext.Name"/> that is used for <see cref="RetrievalEvaluatorContext"/>.
    /// </summary>
    public static string RetrievedContextChunksContextName => "Retrieved Context Chunks (Retrieval)";

    /// <summary>
    /// Gets the context chunks that were retrieved in response to the user request being evaluated.
    /// </summary>
    public IReadOnlyList<string> RetrievedContextChunks { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="RetrievalEvaluatorContext"/> class.
    /// </summary>
    /// <param name="retrievedContextChunks">
    /// The context chunks that were retrieved in response to the user request being evaluated.
    /// </param>
    public RetrievalEvaluatorContext(IEnumerable<string> retrievedContextChunks)
        : base(
            name: RetrievedContextChunksContextName,
            contents: [.. retrievedContextChunks.Select(c => new TextContent(c))])
    {
        RetrievedContextChunks = [.. retrievedContextChunks];
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RetrievalEvaluatorContext"/> class.
    /// </summary>
    /// <param name="retrievedContextChunks">
    /// The context chunks that were retrieved in response to the user request being evaluated.
    /// </param>
    public RetrievalEvaluatorContext(params string[] retrievedContextChunks)
        : this(retrievedContextChunks as IEnumerable<string>)
    {
    }
}
