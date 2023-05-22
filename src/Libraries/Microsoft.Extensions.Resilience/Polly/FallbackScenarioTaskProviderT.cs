// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Microsoft.Extensions.Resilience.Options;
using Polly.Fallback;

namespace Microsoft.Extensions.Resilience;

#pragma warning disable SA1649 // File name should match first type name

/// <summary>
/// A delegate that executes in the fallback scenarios when the initial execution encounters a failure.
/// </summary>
/// <typeparam name="TResult">Type of the result returned.</typeparam>
/// <param name="args">Arguments for the fallback scenario task provider. See <see cref="FallbackScenarioTaskArguments"/>.</param>
/// <returns>Result of a fallback task.</returns>
/// <seealso cref="FallbackPolicyOptions{TResult}"/>
/// <seealso cref="AsyncFallbackPolicy{TResult}"/>
public delegate Task<TResult> FallbackScenarioTaskProvider<TResult>(FallbackScenarioTaskArguments args);
