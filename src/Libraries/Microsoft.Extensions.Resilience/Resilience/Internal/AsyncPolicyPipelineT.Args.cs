// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Polly;

namespace Microsoft.Extensions.Resilience.Internal;

internal sealed partial class AsyncPolicyPipeline<TResult>
{
    /// <summary>
    /// Structure with the arguments of the execute methods.
    /// </summary>
    private sealed class PollyExecuteAsyncArguments
    {
        public Func<Context, CancellationToken, Task<TResult>>? Action { get; set; }

        public Context? Context { get; set; }

        public bool ContinueOnCapturedContext { get; set; }

        public CancellationToken CancellationToken { get; set; } = CancellationToken.None;

        public Policies? Policies { get; set; }
    }

    private sealed class Policies
    {
        public Policies(IReadOnlyList<IAsyncPolicy<TResult>> policies)
        {
#pragma warning disable S109 // Magic numbers should not be used
            Policy0 = GetPolicy(policies, 0);
            Policy1 = GetPolicy(policies, 1);
            Policy2 = GetPolicy(policies, 2);
            Policy3 = GetPolicy(policies, 3);
            Policy4 = GetPolicy(policies, 4);
#pragma warning restore S109 // Magic numbers should not be used
        }

        public IAsyncPolicy<TResult>? Policy0 { get; }

        public IAsyncPolicy<TResult>? Policy1 { get; }

        public IAsyncPolicy<TResult>? Policy2 { get; }

        public IAsyncPolicy<TResult>? Policy3 { get; }

        public IAsyncPolicy<TResult>? Policy4 { get; }

        private static IAsyncPolicy<TResult>? GetPolicy(IReadOnlyList<IAsyncPolicy<TResult>> policies, int index)
        {
            if (index >= policies.Count)
            {
                return null;
            }

            return policies[index];
        }
    }
}
