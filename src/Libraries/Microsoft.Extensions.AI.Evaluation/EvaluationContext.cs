// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading;

namespace Microsoft.Extensions.AI.Evaluation;

/// <summary>
/// An <see langword="abstract"/> base class that models additional contextual information (beyond that which is
/// available in the conversation history) or other data that an <see cref="IEvaluator"/> may need to accurately
/// evaluate supplied responses.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="EvaluationContext"/> objects are intended to be simple data containers that contain the contextual
/// information required for evaluation and little (if any) behavior.
/// </para>
/// <para>
/// An <see cref="IEvaluator"/> that needs additional contextual information can require that callers should include an
/// instance of a specific derived <see cref="EvaluationContext"/> (containing the required contextual information)
/// when they call
/// <see cref="IEvaluator.EvaluateAsync(IEnumerable{ChatMessage}, ChatResponse, ChatConfiguration?, IEnumerable{EvaluationContext}?, CancellationToken)"/>.
/// </para>
/// <para>
/// Derived implementations of <see cref="EvaluationContext"/> are free to include any additional properties as needed.
/// However, the expectation is that the <see cref="Contents"/> property will always return a collection of
/// <see cref="AIContent"/>s that represent <b>all</b> the contextual information that is modeled by the
/// <see cref="EvaluationContext"/>.
/// </para>
/// <para>
/// This is because an <see cref="IEvaluator"/> can (optionally) choose to record any <see cref="EvaluationContext"/>s
/// that it used, in the <see cref="EvaluationMetric.Context"/> property of each <see cref="EvaluationMetric"/> that it
/// produces. When <see cref="EvaluationMetric"/>s are serialized (for example, as part of the result storage and
/// report generation functionality available in the Microsoft.Extensions.AI.Evaluation.Reporting NuGet package), the
/// <see cref="EvaluationContext"/>s recorded within the <see cref="EvaluationMetric.Context"/> will also be
/// serialized. However, for each such <see cref="EvaluationContext"/>, only the information captured within
/// <see cref="Contents"/> will be serialized. Any information that is (only) present in custom derived
/// properties will not be serialized. Therefore, in order to ensure that the contextual information included as part
/// of an <see cref="EvaluationContext"/> is stored and reported accurately, it is important to ensure that the
/// <see cref="Contents"/> property returns a collection of <see cref="AIContent"/>s that represent <b>all</b> the
/// contextual information that is modeled by the <see cref="EvaluationContext"/>.
/// </para>
/// </remarks>
#pragma warning disable S1694 // An abstract class should have both abstract and concrete methods
public abstract class EvaluationContext
#pragma warning restore S1694
{
    /// <summary>
    /// Gets or sets the name for this <see cref="EvaluationContext"/>.
    /// </summary>
    public string Name { get; set; }

#pragma warning disable CA2227
    // CA2227: Collection properties should be read only.
    // We disable this warning because we want this property to be fully mutable for serialization purposes and for
    // general convenience.

    /// <summary>
    /// Gets or sets a list of <see cref="AIContent"/> objects that include all the information present in this
    /// <see cref="EvaluationContext"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This property allows decomposition of the information present in an <see cref="EvaluationContext"/> into
    /// <see cref="TextContent"/> objects for text, <see cref="DataContent"/> or <see cref="UriContent"/> objects for
    /// images, and other similar <see cref="AIContent"/> objects for other modalities such as audio and video in the
    /// future.
    /// </para>
    /// <para>
    /// For simple <see cref="EvaluationContext"/>s that only contain text, this property can return a
    /// <see cref="TextContent"/> object that includes the contained text.
    /// </para>
    /// <para>
    /// Derived implementations of <see cref="EvaluationContext"/> are free to include any additional properties as
    /// needed. However, the expectation is that the <see cref="Contents"/> property will always return a collection of
    /// <see cref="AIContent"/>s that represent <b>all</b> the contextual information that is modeled by the
    /// <see cref="EvaluationContext"/>.
    /// </para>
    /// <para>
    /// This is because an <see cref="IEvaluator"/> can (optionally) choose to record any
    /// <see cref="EvaluationContext"/>s that it used, in the <see cref="EvaluationMetric.Context"/> property of each
    /// <see cref="EvaluationMetric"/> that it produces. When <see cref="EvaluationMetric"/>s are serialized (for
    /// example, as part of the result storage and report generation functionality available in the
    /// Microsoft.Extensions.AI.Evaluation.Reporting NuGet package), the <see cref="EvaluationContext"/>s recorded
    /// within the <see cref="EvaluationMetric.Context"/> will also be serialized. However, for each such
    /// <see cref="EvaluationContext"/>, only the information captured within <see cref="Contents"/> will be
    /// serialized. Any information that is (only) present in custom derived properties will not be serialized.
    /// Therefore, in order to ensure that the contextual information included as part of an
    /// <see cref="EvaluationContext"/> is stored and reported accurately, it is important to ensure that the
    /// <see cref="Contents"/> property returns a collection of <see cref="AIContent"/>s that represent <b>all</b> the
    /// contextual information that is modeled by the <see cref="EvaluationContext"/>.
    /// </para>
    /// </remarks>
    /// <returns>
    /// A list of <see cref="AIContent"/> objects that include all the information present in this
    /// <see cref="EvaluationContext"/>.
    /// </returns>
    public IList<AIContent> Contents { get; set; }
#pragma warning restore CA2227

    /// <summary>
    /// Initializes a new instance of the <see cref="EvaluationContext"/> class.
    /// </summary>
    /// <param name="name">The name of the <see cref="EvaluationContext"/>.</param>
    /// <param name="contents">
    /// The contents of the <see cref="EvaluationContext"/>. (See <see cref="Contents"/>.)
    /// </param>
    protected EvaluationContext(string name, IEnumerable<AIContent> contents)
    {
        Name = name;
        Contents = [.. contents];
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EvaluationContext"/> class.
    /// </summary>
    /// <param name="name">The name of the <see cref="EvaluationContext"/>.</param>
    /// <param name="contents">
    /// The contents of the <see cref="EvaluationContext"/>. (See <see cref="Contents"/>.)
    /// </param>
    protected EvaluationContext(string name, params AIContent[] contents)
        : this(name, contents as IEnumerable<AIContent>)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EvaluationContext"/> class.
    /// </summary>
    /// <param name="name">The name of the <see cref="EvaluationContext"/>.</param>
    /// <param name="content">
    /// The content of the <see cref="EvaluationContext"/>. (See <see cref="Contents"/>.)
    /// </param>
    protected EvaluationContext(string name, string content)
        : this(name, contents: new TextContent(content))
    {
    }
}
