// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Represents an <see cref="AIFunction"/> that can be described to an AI service and invoked, but for which
/// the invoker should obtain user approval before the function is actually invoked.
/// </summary>
/// <remarks>
/// This class simply augments an <see cref="AIFunction"/> with an indication that approval is required before invocation.
/// It does not enforce the requirement for user approval; it is the responsibility of the invoker to obtain that approval before invoking the function.
/// </remarks>
[Experimental("MEAI001")]
public sealed class ApprovalRequiredAIFunction : DelegatingAIFunction
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ApprovalRequiredAIFunction"/> class.
    /// </summary>
    /// <param name="innerFunction">The <see cref="AIFunction"/> represented by this instance.</param>
    /// <exception cref="ArgumentNullException"><paramref name="innerFunction"/> is <see langword="null"/>.</exception>
    public ApprovalRequiredAIFunction(AIFunction innerFunction)
        : base(innerFunction)
    {
    }
}
