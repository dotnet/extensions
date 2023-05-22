// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Microsoft.Extensions.Resilience.Options;
using Polly.Fallback;

namespace Microsoft.Extensions.Resilience;

/// <summary>
/// A delegate that executes in the fallback scenarios when the initial execution encounters a failure.
/// </summary>
/// <param name="args">Arguments for the fallback scenario task provider. See <see cref="FallbackScenarioTaskArguments"/>.</param>
/// <returns>A task representing asynchronous operation.</returns>
/// <seealso cref="FallbackPolicyOptions"/>
/// <seealso cref="AsyncFallbackPolicy"/>
public delegate Task FallbackScenarioTaskProvider(FallbackScenarioTaskArguments args);
