// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Polly;

namespace Microsoft.Extensions.Resilience.Options;

#pragma warning disable SA1649 // File name should match first type name

/// <summary>
/// Lexical interface for all generic policy arguments.
/// Do not use outside Argument struct header to avoid overhead.
/// </summary>
/// <typeparam name="TResult">The type of the result handled by the policy.</typeparam>
internal interface IPolicyEventArguments<TResult> : IPolicyEventArguments
{
    /// <summary>
    /// Gets the result of the action executed by the policy.
    /// </summary>
    public DelegateResult<TResult> Result { get; }
}
