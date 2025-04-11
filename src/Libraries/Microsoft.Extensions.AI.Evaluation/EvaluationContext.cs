// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.Extensions.AI.Evaluation;

/// <summary>
/// A base class that represents additional contextual information (beyond that which is available in the conversation
/// history) that an <see cref="IEvaluator"/> may need to accurately evaluate a supplied response. 
/// </summary>
#pragma warning disable S1694 // An abstract class should have both abstract and concrete methods
public abstract class EvaluationContext
#pragma warning restore S1694
{
    /// <summary>
    /// Returns a list of <see cref="AIContent"/> objects that include all the information present in this
    /// <see cref="EvaluationContext"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This function allows us to decompose the information present in an <see cref="EvaluationContext"/> into
    /// <see cref="TextContent"/> objects for text, <see cref="DataContent"/> or <see cref="UriContent"/> objects for
    /// images, and other similar <see cref="AIContent"/> objects for other modalities such as audio and video in the
    /// future.
    /// </para>
    /// <para>
    /// For simple <see cref="EvaluationContext"/>s that only contain text, this function can return a single
    /// <see cref="TextContent"/> object that includes the contained text.
    /// </para>
    /// </remarks>
    /// <returns>
    /// A list of <see cref="AIContent"/> objects that include all the information present in this
    /// <see cref="EvaluationContext"/>.
    /// </returns>
    public abstract IReadOnlyList<AIContent> GetContents();
}
