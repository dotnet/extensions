// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Http.Resilience.Internal;

/// <summary>
/// Arguments of the <c>OnRetryBudgetExhausted</c> resilience event.
/// </summary>
/// <param name="AttemptNumber">The zero-based number of the attempt whose retry was denied.</param>
internal readonly record struct OnRetryBudgetExhaustedArguments(int AttemptNumber);
